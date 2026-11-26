using System.Globalization;
using System.Linq.Expressions;
using HttpBridge.Core.OData;

namespace HttpBridge.Tests;

public class FilterQueryBuilderTests
{
    private class Client
    {
        public string FirstName { get; set; }
    }

    private enum Status { Open, Paid }

    private class Invoice
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public bool Active { get; set; }
        public DateTime Created { get; set; }
        public Client Client { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset Updated { get; set; }
        public Status Status { get; set; }
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

    [Fact]
    public void Not_negates_member_and_binary()
    {
        Assert.Equal("not active", Build(x => !x.Active));
        Assert.Equal("not (age gt 5 or name eq null)", Build(x => !(x.Age > 5 || x.Name == null)));
    }

    [Fact]
    public void Single_quotes_are_doubled()
    {
        string name = "x' or name ne '";
        Assert.Equal("name eq 'x'' or name ne '''", Build(x => x.Name == name));
        Assert.Equal("name eq 'O''Brien'", Build(x => x.Name == "O'Brien"));
    }

    [Fact]
    public void List_and_hashset_use_in()
    {
        List<string> list = ["a", "b"];
        HashSet<string> set = ["c"];
        Assert.Equal("name in ('a','b')", Build(x => list.Contains(x.Name)));
        Assert.Equal("name in ('c')", Build(x => set.Contains(x.Name)));
    }

    [Fact]
    public void Property_of_captured_instance_is_evaluated()
    {
        Assert.Equal("name eq 'captured'", Build(x => x.Name == Holder.Name));
    }

    private static readonly HolderType Holder = new() { Name = "captured" };

    private class HolderType
    {
        public string Name { get; init; }
    }

    [Fact]
    public void Values_use_invariant_culture_and_utc()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("uk-UA");
        try
        {
            decimal price = 1.5m;
            DateTimeOffset updated = new(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
            DateTime unspecified = new(2020, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
            Assert.Equal("price gt 1.5", Build(x => x.Price > price));
            Assert.Equal("updated gt 2020-01-02T01:04:05.0000000Z", Build(x => x.Updated > updated));
            Assert.Equal("created gt 2020-01-02T03:04:05.0000000Z", Build(x => x.Created > unspecified));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Enum_is_quoted_name()
    {
        Assert.Equal("status eq 'Paid'", Build(x => x.Status == Status.Paid));
        Status paid = Status.Paid;
        Assert.Equal("status ne 'Paid'", Build(x => x.Status != paid));
    }
}
