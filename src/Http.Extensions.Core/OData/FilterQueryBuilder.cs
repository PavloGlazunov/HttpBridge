using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Http.Extensions.Core.OData;

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
            UnaryExpression unary => ParseExpression(unary.Operand),
            _ => throw new NotSupportedException($"Unsupported expression: {expression.NodeType}")
        };
    }

    private static string ParseBinary(BinaryExpression expr)
    {
        string left = FormatBinaryExpression(expr.Left, expr.NodeType);
        string right = FormatBinaryExpression(expr.Right, expr.NodeType);
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
        if (member.Expression == null)
        {
            return FormatConstant(GetStaticMemberValue(member));
        }

        if (member.Expression is ConstantExpression closure)
        {
            object container = closure.Value;
            object value = ((FieldInfo)member.Member).GetValue(container);
            return FormatConstant(value);
        }

        try
        {
            object value = GetValue(member);
            return FormatConstant(value);
        }
        catch
        {
            return null;
        }
    }

    private static object GetStaticMemberValue(MemberExpression member)
    {
        return member.Member switch
        {
            FieldInfo field => field.GetValue(null),
            PropertyInfo prop => prop.GetValue(null),
            _ => throw new NotSupportedException($"Unsupported static member: {member.Member.Name}")
        };
    }

    private static string FormatConstant(object value)
    {
        return value switch
        {
            null => "null",
            string s => $"'{s}'",
            DateTime dt => $"{dt.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffffffZ}",
            bool b => b.ToString().ToLower(),
            Guid g => $"'{g}'",
            _ => value.ToString()
        };
    }

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
