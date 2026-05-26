using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Models.ViewModels;
using RazorShop.Web.Slices.Admin.Settings;
using LinqKit;

using Size = RazorShop.Data.Entities.Size;

namespace RazorShop.Web.Apis.Settings;

public static class SettingsApis
{
    private const string _apiCategories = "/admin/settings/categories";
    private const string _apiSizes = "/admin/settings/sizes";

    public static void SettingsApi(this WebApplication app)
    {
        app.MapGet($"{_apiCategories}", (HttpContext http) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.RazorSlice<Categories>();
            }

            return Results.RazorSlice<Pages.Admin.Settings.Categories>();
        }).RequireAuthorization("AdminOnly");

        app.MapGet($"{_apiCategories}/table", async (HttpRequest request, RazorShopDbContext db) =>
        {
            var dtParams = GetDatatableParameters(request, CategorySortColumns, "Id");

            var vm = await GetPaginatedAdminCategories(db, dtParams.Search!, dtParams.Take, dtParams.Skip, dtParams.Sort!, dtParams.SortDirection!);

            List<object> categoriesTableVm = new();
            foreach (var product in vm.AdminCategories)
            {
                categoriesTableVm.Add(new {
                    product.Id,
                    product.Name,
                });
            }

            return Results.Json(new {
                dtParams.Draw,
                recordsTotal = vm.TotalCount,
                recordsFiltered = vm.FilteredCount,
                data = categoriesTableVm
            });
        }).RequireAuthorization("AdminOnly");

        app.MapGet($"{_apiCategories}/modal/new", (HttpContext http, IAntiforgery antiforgery) =>
        {
            var vm = new AdminCategoryVm();
            var token = antiforgery.GetAndStoreTokens(http);
            vm!.AdminCategoryFormAntiForgeryToken = token.RequestToken!;

            return Results.RazorSlice<CategoryNew, AdminCategoryVm>(vm);
        }).RequireAuthorization("AdminOnly");

        app.MapPost($"{_apiCategories}/modal/new", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            await db.Categories!.AddAsync(new Category { Name = form["name"], StatusId = 1, Created = DateTime.Now });
            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        app.MapGet($"{_apiCategories}/modal/edit/{{id}}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, IAntiforgery antiforgery, int id) =>
        {
            var category = await db.Categories!.FirstAsync(p => p.Id == id);

            var vm = new AdminCategoryVm();
            vm.Id = category.Id;
            vm.Name = category.Name;
            vm.StatusId = category.StatusId;

            var statuses = (IEnumerable<Status>)cache.Get("statuses")!;
            foreach (var status in statuses)
                vm.AdminStatuses!.Add(new AdminStatusVm { Id = status.Id, Name = status.Name });

            var token = antiforgery.GetAndStoreTokens(http);
            vm!.AdminCategoryFormAntiForgeryToken = token.RequestToken!;

            return Results.RazorSlice<CategoryEdit, AdminCategoryVm>(vm);
        }).RequireAuthorization("AdminOnly");

        app.MapPost($"{_apiCategories}/modal/edit/{{id}}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, IMemoryCache cache, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            if (!int.TryParse(form["statusDd"], out var statusId))
                return Results.BadRequest();

            var category = await db.Categories!.FindAsync(id);
            category!.Name = form["name"];
            category.StatusId = statusId;
            category.Updated = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var categories = await db.Categories.ToListAsync();
            var options = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
            cache.Set("categories", categories, options);

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        app.MapGet($"{_apiSizes}", (HttpContext http) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.RazorSlice<Sizes>();
            }

            return Results.RazorSlice<Pages.Admin.Settings.Sizes>();
        }).RequireAuthorization("AdminOnly");

        app.MapGet($"{_apiSizes}/table", async (RazorShopDbContext db, HttpRequest request) =>
        {
            var dtParams = GetDatatableParameters(request, SizeSortColumns, "Id");

            var vm = await GetPaginatedAdminSizes(db, dtParams.Search!, dtParams.Take, dtParams.Skip, dtParams.Sort!, dtParams.SortDirection!);

            List<object> sizesTableVm = new();
            foreach (var product in vm.AdminSizes)
            {
                sizesTableVm.Add(new
                {
                    product.Id,
                    product.Name,
                });
            }

            return Results.Json(new {
                dtParams.Draw,
                recordsTotal = vm.TotalCount,
                recordsFiltered = vm.FilteredCount,
                data = sizesTableVm
            });
        }).RequireAuthorization("AdminOnly");

        app.MapGet($"{_apiSizes}/modal/new", (HttpContext http, RazorShopDbContext db, IMemoryCache cache, IAntiforgery antiforgery) =>
        {
            var vm = new AdminSizeVm();
            var token = antiforgery.GetAndStoreTokens(http);
            vm!.AdminSizeFormAntiForgeryToken = token.RequestToken!;

            var sizeTypes = (IEnumerable<SizeType>)cache.Get("sizeTypes")!;
            foreach (var type in sizeTypes)
                vm.AdminSizeTypes!.Add(new AdminSizeTypeVm { Id = type.Id, Name = type.Name });

            return Results.RazorSlice<SizeNew, AdminSizeVm>(vm);
        }).RequireAuthorization("AdminOnly");

        app.MapPost($"{_apiSizes}/modal/new", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            if (!int.TryParse(form["sizeTypeDd"], out var sizeTypeId))
                return Results.BadRequest();

            var size = new Size();
            size.Name = form["name"];
            size.SizeTypeId = sizeTypeId == 0 ? null : sizeTypeId;
            await db.Sizes!.AddAsync(size);
            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        app.MapGet($"{_apiSizes}/modal/edit/{{id}}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, IAntiforgery antiforgery, int id) =>
        {
            var size = await db.Sizes!.FirstAsync(p => p.Id == id);

            var vm = new AdminSizeVm();
            vm.Id = size.Id;
            vm.Name = size.Name;
            vm.SizeTypeId = size.SizeTypeId;

            var token = antiforgery.GetAndStoreTokens(http);
            vm!.AdminSizeFormAntiForgeryToken = token.RequestToken!;

            var sizeTypes = (IEnumerable<SizeType>)cache.Get("sizeTypes")!;
            foreach (var type in sizeTypes)
                vm.AdminSizeTypes!.Add(new AdminSizeTypeVm { Id = type.Id, Name = type.Name });

            return Results.RazorSlice<SizeEdit, AdminSizeVm>(vm);
        }).RequireAuthorization("AdminOnly");

        app.MapPost($"{_apiSizes}/modal/edit/{{id}}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, IMemoryCache cache, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            if (!int.TryParse(form["sizeTypeDd"], out var sizeTypeId))
                return Results.BadRequest();

            var size = await db.Sizes!.FindAsync(id);
            size!.Name = form["name"];
            size.SizeTypeId = sizeTypeId;
            await db.SaveChangesAsync();

            var sizes = await db.Sizes!.ToListAsync();
            var options = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
            cache.Set("sizes", sizes, options);

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");
    }

    private static async Task<AdminCategoriesVm> GetPaginatedAdminCategories(RazorShopDbContext db, string search, int take, int skip, string sort, string dir)
    {
        var predicate = PredicateBuilder.New<Category>(true);

        if (!string.IsNullOrWhiteSpace(search))
            predicate = predicate.And(i => i.Name!.ToLower().Contains(search.ToLower()));

        return new() {
            AdminCategories = await db.Categories!.AsNoTracking()
            .Where(predicate)
            .Select(c => new AdminCategoryVm { Id = c.Id, Name = c.Name/*, StatusId = o.StatusId */})
            .OrderBy($"{sort} {dir}")
            .Skip(skip)
            .Take(take).ToListAsync(),

            FilteredCount = db.Categories!.Where(predicate).Count(),
            TotalCount = db.Categories!.Where(predicate).Count()
        };
    }

    private static async Task<AdminSizesVm> GetPaginatedAdminSizes(RazorShopDbContext db, string search, int take, int skip, string sort, string dir)
    {
        var predicate = PredicateBuilder.New<Size>(true);

        if (!string.IsNullOrWhiteSpace(search))
            predicate = predicate.And(i => i.Name!.ToLower().Contains(search.ToLower()));

        return new() {
            AdminSizes = await db.Sizes!.AsNoTracking()
            .Where(predicate)
            .Select(s => new AdminSizeVm { Id = s.Id, Name = s.Name/*, StatusId = o.StatusId */})
            .OrderBy($"{sort} {dir}")
            .Skip(skip)
            .Take(take).ToListAsync(),

            FilteredCount = db.Sizes!.Where(predicate).Count(),
            TotalCount = db.Sizes!.Where(predicate).Count()
        };
    }

    private static readonly HashSet<string> CategorySortColumns = new(StringComparer.Ordinal) { "Id", "Name" };
    private static readonly HashSet<string> SizeSortColumns = new(StringComparer.Ordinal) { "Id", "Name" };

    private static DataTablesParameters GetDatatableParameters(HttpRequest request, HashSet<string> allowedSortColumns, string defaultSort)
    {
        var search = request.Query["search[value]"].FirstOrDefault();
        var draw = request.Query["draw"].FirstOrDefault();
        _ = int.TryParse(request.Query["start"].FirstOrDefault(), out var skip);
        if (!int.TryParse(request.Query["length"].FirstOrDefault(), out var take) || take <= 0 || take > 200)
            take = 10;

        _ = int.TryParse(request.Query["order[0][column]"].FirstOrDefault(), out var orderIndex);
        var dirRaw = request.Query["order[0][dir]"].FirstOrDefault();
        var dir = string.Equals(dirRaw, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var sortRaw = request.Query[$"columns[{orderIndex}][name]"].FirstOrDefault();
        var sort = !string.IsNullOrEmpty(sortRaw) && allowedSortColumns.Contains(sortRaw) ? sortRaw : defaultSort;

        return new DataTablesParameters {
            Search = search!,
            Draw = draw!,
            Skip = skip < 0 ? 0 : skip,
            Take = take,
            OrderIndex = orderIndex,
            Sort = sort,
            SortDirection = dir
        };
    }
}

class DataTablesParameters
{
    public string? Search { get; set; }
    public string? Draw { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int OrderIndex { get; set; }
    public string? Sort { get; set; }
    public string? SortDirection { get; set; }
}