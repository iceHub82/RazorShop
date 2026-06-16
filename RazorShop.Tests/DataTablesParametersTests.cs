using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using RazorShop.Web.Apis;
using Xunit;

namespace RazorShop.Tests;

// GetDatatableParameters is a trust boundary: it parses attacker-controllable query
// string into paging/sort. These guard the clamps and the sort allow-list.
public class DataTablesParametersTests
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal) { "Id", "Name" };

    private static HttpRequest Request(Dictionary<string, StringValues> query)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Query = new QueryCollection(query);
        return ctx.Request;
    }

    [Theory]
    [InlineData("500", 10)]  // over the 200 cap -> default 10
    [InlineData("0", 10)]    // non-positive -> default 10
    [InlineData("-5", 10)]   // negative -> default 10
    [InlineData("abc", 10)]  // unparseable -> default 10
    [InlineData("50", 50)]   // valid passes through
    public void Take_is_clamped(string length, int expected)
    {
        var p = ApiUtil.GetDatatableParameters(Request(new() { ["length"] = length }), Allowed, "Id");
        Assert.Equal(expected, p.Take);
    }

    [Fact]
    public void Negative_skip_becomes_zero()
    {
        var p = ApiUtil.GetDatatableParameters(Request(new() { ["start"] = "-10" }), Allowed, "Id");
        Assert.Equal(0, p.Skip);
    }

    [Fact]
    public void Disallowed_sort_column_falls_back_to_default()
    {
        var p = ApiUtil.GetDatatableParameters(
            Request(new() { ["order[0][column]"] = "0", ["columns[0][name]"] = "DROP TABLE" }),
            Allowed, "Id");

        Assert.Equal("Id", p.Sort);
    }

    [Fact]
    public void Allowed_sort_column_and_desc_direction_pass_through()
    {
        var p = ApiUtil.GetDatatableParameters(
            Request(new()
            {
                ["order[0][column]"] = "0",
                ["columns[0][name]"] = "Name",
                ["order[0][dir]"] = "DESC",
            }),
            Allowed, "Id");

        Assert.Equal("Name", p.Sort);
        Assert.Equal("desc", p.SortDirection);
    }
}
