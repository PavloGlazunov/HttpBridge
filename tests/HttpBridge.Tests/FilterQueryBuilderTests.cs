using System.Linq.Expressions;
using HttpBridge.Core.OData;

namespace HttpBridge.Tests;

public class FilterQueryBuilderTests
{
    private class Client
    {
        public string FirstName { get; set; }
    }

    private class Invoice
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public bool Active { get; set; }
        public DateTime Created { get; set; }
        public Client Client { get; set; }
    }

    private static string Build(Expression<Func<Invoice, bool>> filter) => FilterQueryBuilder.CreateFilter(filter);

    [Fact]
    public void Guid_equal_and_not_equal() {
        Assert.Equal("id eq '00000000-0000-0000-0000-000000000000'", Build(x => x.Id == Guid.Empty));
        Assert.Equal("id ne '00000000-0000-0000-0000-000000000000'", Build(x => x.Id != Guid.Empty));
    }

    [Fact]
    public void Date_is_utc_iso() {
        DateTime date = new(1999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        Assert.Equal("created gt 1999-12-31T23:59:59.0000000Z", Build(x => x.Created > date));
    }

    [Fact]
    public void Null_bool_and_number() {
        Assert.Equal("name ne null", Build(x => x.Name != null));
        Assert.Equal("active eq true", Build(x => x.Active == true));
        Assert.Equal("age ge 5", Build(x => x.Age >= 5));
    }

    [Fact]
    public void String_functions() {
        Assert.Equal("contains(name,'test')", Build(x => x.Name.Contains("test")));
        Assert.Equal("startswith(name,'test')", Build(x => x.Name.StartsWith("test")));
        Assert.Equal("endswith(name,'test')", Build(x => x.Name.EndsWith("test")));
    }

    [Fact]
    public void In_operator() {
        string[] names = ["first", "last"];
        Assert.Equal("name in ('first','last')", Build(x => names.Contains(x.Name)));
    }

    [Fact]
    public void Nested_property_is_slash_path() =>
        Assert.Equal("client/firstName eq 'a'", Build(x => x.Client.FirstName == "a"));

    [Fact]
    public void Logical_operators_and_parentheses() {
        Assert.Equal("name ne null and age gt 5", Build(x => x.Name != null && x.Age > 5));
        Assert.Equal("active and (age gt 5 or age lt 1)", Build(x => x.Active && (x.Age > 5 || x.Age < 1)));
    }
}
