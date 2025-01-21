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
                return Results.Extensions.RazorSlice<Categories>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Settings.Categories>();
        }).RequireAuthorization();

        app.MapGet($"{_apiCategories}/table", async (HttpRequest request, RazorShopDbContext db) =>
        {
            var dtParams = GetDatatableParameters(request);

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
        }).RequireAuthorization();

        app.MapGet($"{_apiCategories}/modal/new", (HttpContext http, IAntiforgery antiforgery) =>
        {
            var vm = new AdminCategoryVm();
            var token = antiforgery.GetAndStoreTokens(http);
            vm!.AdminCategoryFormAntiForgeryToken = token.RequestToken!;

            return Results.Extensions.RazorSlice<CategoryNew, AdminCategoryVm>(vm);
        }).RequireAuthorization();

        app.MapPost($"{_apiCategories}/modal/new", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            await db.Categories!.AddAsync(new Category { Name = form["name"] });
            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization();

        app.MapGet($"{_apiCategories}/modal/edit/{{id}}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, int id) =>
        {
            var category = await db.Categories!.FirstAsync(p => p.Id == id);

            var vm = new AdminCategoryVm();
            vm.Id = category.Id;
            vm.Name = category.Name;

            var token = antiforgery.GetAndStoreTokens(http);
            vm!.AdminCategoryFormAntiForgeryToken = token.RequestToken!;

            return Results.Extensions.RazorSlice<CategoryEdit, AdminCategoryVm>(vm);
        }).RequireAuthorization();

        app.MapPost($"{_apiCategories}/modal/edit/{{id}}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, IMemoryCache cache, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            var category = await db.Categories!.FindAsync(id);
            category!.Name = form["name"];
            await db.SaveChangesAsync();

            var categories = await db.Categories.ToListAsync();
            var options = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
            cache.Set("categories", categories, options);

            return Results.Ok();
        }).RequireAuthorization();

        app.MapGet($"{_apiSizes}", (HttpContext http) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Sizes>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Settings.Sizes>();
        }).RequireAuthorization();

        app.MapGet($"{_apiSizes}/table", async (RazorShopDbContext db, HttpRequest request) =>
        {
            var dtParams = GetDatatableParameters(request);

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
        }).RequireAuthorization();

        app.MapGet($"{_apiSizes}/modal/new", (HttpContext http, RazorShopDbContext db, IMemoryCache cache, IAntiforgery antiforgery) =>
        {
            var vm = new AdminSizeVm();
            var token = antiforgery.GetAndStoreTokens(http);
            vm!.AdminSizeFormAntiForgeryToken = token.RequestToken!;

            var sizeTypes = (IEnumerable<SizeType>)cache.Get("sizeTypes")!;
            foreach (var type in sizeTypes)
                vm.AdminSizeTypes!.Add(new AdminSizeTypeVm { Id = type.Id, Name = type.Name });

            return Results.Extensions.RazorSlice<SizeNew, AdminSizeVm>(vm);
        }).RequireAuthorization();

        app.MapPost($"{_apiSizes}/modal/new", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            var size = new Size();
            size.Name = form["name"];
            var sizeTypeId = int.Parse(form["sizeTypeDd"]!);
            size.SizeTypeId = sizeTypeId == 0 ? null : sizeTypeId;
            await db.Sizes!.AddAsync(size);
            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization();

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

            return Results.Extensions.RazorSlice<SizeEdit, AdminSizeVm>(vm);
        }).RequireAuthorization();

        app.MapPost($"{_apiSizes}/modal/edit/{{id}}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, IMemoryCache cache, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            var size = await db.Sizes!.FindAsync(id);
            size!.Name = form["name"];
            size!.SizeTypeId = int.Parse(form["sizeTypeDd"]!);
            await db.SaveChangesAsync();

            var sizes = await db.Sizes!.ToListAsync();
            var options = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
            cache.Set("sizes", sizes, options);

            return Results.Ok();
        }).RequireAuthorization();
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

    private static DataTablesParameters GetDatatableParameters(HttpRequest request)
    {
        var search = request.Query["search[value]"].FirstOrDefault();
        var draw = request.Query["draw"].FirstOrDefault();
        var skip = int.Parse(request.Query["start"].FirstOrDefault() ?? "0");
        var take = int.Parse(request.Query["length"].FirstOrDefault() ?? "10");

        var orderIndex = int.Parse(request.Query["order[0][column]"].FirstOrDefault() ?? "0");
        var dir = request.Query["order[0][dir]"].FirstOrDefault() ?? "asc";
        var sort = request.Query[$"columns[{orderIndex}][name]"].FirstOrDefault();

        return new DataTablesParameters {
            Search = search!,
            Draw = draw!,
            Skip = skip,
            Take = take,
            OrderIndex = orderIndex,
            Sort = sort!,
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