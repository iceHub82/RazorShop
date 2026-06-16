using RazorShop.Web.Apis.Admin;
using Xunit;

namespace RazorShop.Tests;

// Exercises GetPaginatedAdminProducts against real SQLite: the (sort, dir) switch,
// the search filter, paging, and the FilteredCount/TotalCount dedup.
public class PaginationTests
{
    [Fact]
    public async Task Unfiltered_returns_all_seeded_products_with_matching_counts()
    {
        using var db = new TestDb();

        var vm = await AdminApis.GetPaginatedAdminProducts(db.Context, "", 10, 0, "Id", "asc");

        Assert.Equal(6, vm.AdminProducts!.Count);   // seed = 6 products
        Assert.Equal(6, vm.FilteredCount);
        Assert.Equal(6, vm.TotalCount);             // both come from the same count now
        Assert.Equal(1, vm.AdminProducts[0].Id);    // ascending by Id
    }

    [Fact]
    public async Task Sort_by_name_descending_orders_correctly()
    {
        using var db = new TestDb();

        var vm = await AdminApis.GetPaginatedAdminProducts(db.Context, "", 10, 0, "Name", "desc");
        var names = vm.AdminProducts!.Select(p => p.Name).ToList();

        Assert.Equal(names.OrderByDescending(n => n, StringComparer.Ordinal).ToList(), names);
    }

    [Fact]
    public async Task Search_filters_count_and_take_limits_page()
    {
        using var db = new TestDb();

        var vm = await AdminApis.GetPaginatedAdminProducts(db.Context, "drawing", 1, 0, "Id", "asc");

        Assert.Single(vm.AdminProducts!);    // take = 1
        Assert.Equal(3, vm.FilteredCount);   // "Drawing 1..3"
        Assert.Equal(3, vm.TotalCount);
    }
}
