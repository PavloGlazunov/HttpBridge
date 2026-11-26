using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace HttpBridge.Core.OData;

public static class FilterQueryBuilder
{
    public static string CreateFilter<T>(Expression<Func<T, bool>> expression)
    {
        return ParseExpression(expression.Body);
    }

    private static string ParseExpression(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => ParseBinary(binary),
            MethodCallExpression methodCall => ParseMethodCall(methodCall),
            MemberExpression member => TryEvaluate(member) ?? GetMemberPath(member),
            ConstantExpression constant => FormatConstant(constant.Value),
            UnaryExpression unary => ParseUnary(unary),
            _ => throw new NotSupportedException($"Unsupported expression: {expression.NodeType}")
        };
    }

    private static string ParseUnary(UnaryExpression unary)
    {
        return unary.NodeType switch
        {
            ExpressionType.Not when unary.Operand is BinaryExpression => $"not ({ParseExpression(unary.Operand)})",
            ExpressionType.Not => $"not {ParseExpression(unary.Operand)}",
            ExpressionType.Negate or ExpressionType.NegateChecked => $"-{ParseExpression(unary.Operand)}",
            ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs or ExpressionType.Quote =>
                ParseExpression(unary.Operand),
            _ => throw new NotSupportedException($"Unsupported unary expression: {unary.NodeType}")
        };
    }

    private static string ParseBinary(BinaryExpression expr)
    {
        // x.Status == Status.Paid compiles to Convert(x.Status) == 1; turn the 1 back into the enum value.
        Type enumType = EnumOperandType(expr.Left) ?? EnumOperandType(expr.Right);
        Expression leftSide = enumType is not null && EnumOperandType(expr.Left) is null
            ? Expression.Constant(GetValue(Expression.Convert(expr.Left, enumType)), enumType)
            : expr.Left;
        Expression rightSide = enumType is not null && EnumOperandType(expr.Right) is null
            ? Expression.Constant(GetValue(Expression.Convert(expr.Right, enumType)), enumType)
            : expr.Right;

        string left = FormatBinaryExpression(leftSide, expr.NodeType);
        string right = FormatBinaryExpression(rightSide, expr.NodeType);
        string op = GetLogicalOperator(expr.NodeType) ?? GetOperator(expr.NodeType);

        return $"{left} {op} {right}";
    }

    private static string FormatBinaryExpression(Expression side, ExpressionType parentType)
    {
        if (side is BinaryExpression childBinary)
        {
            bool isParenthesesRequired = IsParenthesesRequired(parentType, childBinary.NodeType);
            string innerExpression = ParseExpression(childBinary);
            return isParenthesesRequired ? $"({innerExpression})" : innerExpression;
        }

        return ParseExpression(side);
    }

    private static bool IsParenthesesRequired(ExpressionType parent, ExpressionType child)
    {
        if (parent == child)
        {
            return false;
        }

        if (parent == ExpressionType.AndAlso && child == ExpressionType.OrElse)
        {
            return true;
        }

        return false;
    }

    private static string GetLogicalOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.AndAlso => "and",
            ExpressionType.OrElse => "or",
            _ => null
        };
    }

    private static string ParseMethodCall(MethodCallExpression expr)
    {
        if (expr.Object is null && expr.Method.Name == nameof(Enumerable.Contains) && expr.Arguments.Count == 2)
        {
            string property = ParseExpression(expr.Arguments[1]);
            var collection = (IEnumerable)GetValue(StripSpanConversion(expr.Arguments[0]));
            string[] values = collection.Cast<object>()
                .Select(FormatConstant)
                .ToArray();
            return $"{property} in ({string.Join(",", values)})";
        }

        // list.Contains(x.Prop), hashSet.Contains(x.Prop), ...
        if (expr.Object is not null
            && expr.Object.Type != typeof(string)
            && typeof(IEnumerable).IsAssignableFrom(expr.Object.Type)
            && expr.Method.Name == nameof(Enumerable.Contains)
            && expr.Arguments.Count == 1)
        {
            string property = ParseExpression(expr.Arguments[0]);
            var collection = (IEnumerable)GetValue(expr.Object);
            return $"{property} in ({string.Join(",", collection.Cast<object>().Select(FormatConstant))})";
        }

        if (expr.Method.Name is nameof(string.ToLower) or nameof(string.ToUpper))
        {
            string property = ExtractPropertyName(expr.Object);
            string methodName = expr.Method.Name.ToLower();
            return $"{methodName}({property})";
        }

        if (expr.Method.DeclaringType == typeof(string))
        {
            string funcName = expr.Method.Name switch
            {
                nameof(string.Contains) => "contains",
                nameof(string.StartsWith) => "startswith",
                nameof(string.EndsWith) => "endswith",
                _ => throw new NotSupportedException($"Unsupported string method: {expr.Method.Name}")
            };

            if (expr.Object is not null)
            {
                string left = ParseExpression(expr.Object);
                string right = ParseExpression(expr.Arguments[0]);
                return $"{funcName}({left},{right})";
            }

            if (expr.Arguments.Count == 1 && expr.Arguments[0] is MemberExpression)
            {
                string left = ParseExpression(expr.Arguments[0]);
                return $"{funcName}({left})";
            }
        }

        throw new NotSupportedException($"Unsupported method call: {expr.Method.Name}");
    }

    private static string GetOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "eq",
            ExpressionType.NotEqual => "ne",
            ExpressionType.GreaterThan => "gt",
            ExpressionType.GreaterThanOrEqual => "ge",
            ExpressionType.LessThan => "lt",
            ExpressionType.LessThanOrEqual => "le",
            _ => throw new NotSupportedException($"Unsupported operator: {nodeType}")
        };
    }

    // C# 14 binds array.Contains(x) to MemoryExtensions.Contains(ReadOnlySpan, x); drop the implicit span conversion
    // so the collection can be evaluated as a plain array/list.
    private static Expression StripSpanConversion(Expression expr)
    {
        return expr switch
        {
            MethodCallExpression { Method.Name: "op_Implicit", Arguments: [var source] } => source,
            UnaryExpression { NodeType: ExpressionType.Convert } unary when unary.Type.IsValueType => unary.Operand,
            _ => expr
        };
    }

    private static object GetValue(Expression expr)
    {
        if (expr is ConstantExpression constant)
        {
            return constant.Value;
        }

        LambdaExpression lambda = Expression.Lambda(expr);
        Delegate compiled = lambda.Compile();
        return compiled.DynamicInvoke();
    }

    private static string TryEvaluate(MemberExpression member)
    {
        if (member.Expression is null or ConstantExpression)
        {
            object target = (member.Expression as ConstantExpression)?.Value;
            return FormatConstant(GetMemberValue(member.Member, target));
        }

        try
        {
            return FormatConstant(GetValue(member));
        }
        catch
        {
            return null; // depends on the lambda parameter, rendered as a property path instead
        }
    }

    private static object GetMemberValue(MemberInfo member, object target)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(target),
            PropertyInfo prop => prop.GetValue(target),
            _ => throw new NotSupportedException($"Unsupported member: {member.Name}")
        };
    }

    private static Type EnumOperandType(Expression expression)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            Type type = Nullable.GetUnderlyingType(unary.Operand.Type) ?? unary.Operand.Type;
            if (type.IsEnum)
            {
                return type;
            }
        }

        return null;
    }

    private static string FormatConstant(object value)
    {
        return value switch
        {
            null => "null",
            string s => $"'{s.Replace("'", "''")}'",
            DateTime dt => FormatDate(dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime()),
            DateTimeOffset dto => FormatDate(dto.UtcDateTime),
            bool b => b ? "true" : "false",
            Guid g => $"'{g}'",
            Enum e => $"'{e}'",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static string FormatDate(DateTime utc) => utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);

    private static string GetMemberPath(MemberExpression member)
    {
        var stack = new Stack<string>();
        Expression current = member;

        while (current is MemberExpression m)
        {
            stack.Push(CamelCase(m.Member.Name));
            current = m.Expression;
        }

        return string.Join("/", stack);
    }

    private static string CamelCase(string name)
    {
        return string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static string ExtractPropertyName(Expression propertyExpression)
    {
        if (propertyExpression is MemberExpression memberExpression)
        {
            return GetMemberPath(memberExpression);
        }

        throw new NotSupportedException($"Unsupported expression type: {propertyExpression.Type.Name}");
    }
}
