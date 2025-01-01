using System.Security.Claims;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Slices.Admin;
using RazorShop.Web.Models.ViewModels;
using LinqKit;
using SixLabors.ImageSharp.Formats.Webp;

using Image = SixLabors.ImageSharp.Image;
using Size = SixLabors.ImageSharp.Size;

namespace RazorShop.Web.Apis;

public static class AdminApis
{
    public static void AdminApi(this WebApplication app)
    {
        app.MapGet("/Admin", (HttpContext http) => {

            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Home>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Home>();
        }).RequireAuthorization();

        app.MapGet("/Admin/Orders", (HttpContext http) => {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Orders>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Orders>();
        }).RequireAuthorization();

        app.MapGet("/Admin/Products", (HttpContext http) => {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Products>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Products>();
        }).RequireAuthorization();

        app.MapGet("/admin/products-table", async (RazorShopDbContext db, HttpRequest request, IMemoryCache cache) =>
        {
            var dtParams = GetDatatableParameters(request);

            var vm = await GetPaginatedAdminProducts(db, dtParams.Search!, dtParams.Take, dtParams.Skip, dtParams.Sort!, dtParams.SortDirection!);

            List<object> productTableVm = new();
            foreach (var product in vm.AdminProducts)
            {
                productTableVm.Add(new {
                    product.Id,
                    product.Name,
                });
            }

            return Results.Json(new {
                dtParams.Draw,
                recordsTotal = vm.TotalCount,
                recordsFiltered = vm.FilteredCount,
                data = productTableVm
            });
        }).RequireAuthorization();

        app.MapGet("/admin/product-modal/{id}", async (HttpContext http, IAntiforgery antiforgery, RazorShopDbContext db, int id) =>
        {
            var product = await db.Products!.Where(p => p.Id == id).Include(p => p.ProductSizes)!.ThenInclude(p => p.Size).FirstAsync();

            var vm = new AdminProductVm();
            vm.Id = product.Id;
            vm.Name = product.Name;
            vm.Price = $"{product.Price:#.00}";
            vm.Description = product.Description;
            vm.ShortDescription = product.ShortDescription;

            var token1 = antiforgery.GetAndStoreTokens(http);
            vm!.AdminProductFormAntiForgeryToken = token1.RequestToken;
            var token2 = antiforgery.GetAndStoreTokens(http);
            vm!.AdminProductFormMainImageAntiForgeryToken = token2.RequestToken;            

            return Results.Extensions.RazorSlice<ProductModal, AdminProductVm>(vm);
        }).RequireAuthorization();

        app.MapPost("/admin/product/edit/{id}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var product = await db.Products!.FindAsync(id);

            var form = await http.Request.ReadFormAsync();

            product!.Name = form["name"]; ;
            product.ShortDescription = form["shortDescription"];
            product.Description = form["description"];

            if (form.TryGetValue("price", out var priceValue) && decimal.TryParse(priceValue, out var price))
                product.Price = price;
            else
                product.Price = 0m;

            await db.SaveChangesAsync();

            product = await db.Products.Where(p => p.Id == id).Include(p => p.ProductSizes)!.ThenInclude(p => p.Size).FirstAsync();

            var vm = new AdminProductVm();
            vm.Id = product.Id;
            vm.Name = product.Name;
            vm.Price = $"{product.Price:#.00}";
            vm.Description = product.Description;
            vm.ShortDescription = product.ShortDescription;

            return Results.Extensions.RazorSlice<ProductModal, AdminProductVm>(vm);
        }).RequireAuthorization();

        app.MapPost("/admin/product/upload-main/{id}", async (IWebHostEnvironment env, HttpContext http, IFormFile file, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var uploadPath = $"{env.WebRootPath}\\products\\{id}";
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var sizes = new List<(string type, int width, int height, int quality)> {
                ("thumbnail", 80, 100, 65),
                ("listing", 260, 330, 75),
                ("product", 600, 740, 80),
                //("zoom", 1024, 1200)
            };

            foreach (var (type, width, height, quality) in sizes)
            {
                var outputPath = Path.Combine(uploadPath, $"{id}_{type}.webp");

                var stream = file.OpenReadStream();

                using (var image = await Image.LoadAsync(stream))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Crop
                    }));

                    await image.SaveAsync(outputPath, new WebpEncoder { Quality = quality });
                }

                stream.Position = 0;
            }

            return Results.Ok();
        }).RequireAuthorization();


        app.MapGet("/Login", () => { 
            return Results.Extensions.RazorSlice<Pages.Admin.Login>();
        });

        app.MapPost("/Login", async (HttpContext context) =>
        {
            var username = context.Request.Form["username"];
            var password = context.Request.Form["password"];

            // Dummy authentication logic
            if (username == "admin" && password == "password")
            {
                var claims = new List<Claim> { 
                    new(ClaimTypes.Name, username!), 
                    new(ClaimTypes.Role, "Admin") 
                };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await context.SignInAsync("MyCookieAuth", claimsPrincipal);

                return Results.Extensions.RazorSlice<Pages.Admin.Home>();
            }

            return Results.Redirect("/Login");
        });
    }

    private static async Task<AdminProductsVm> GetPaginatedAdminProducts(RazorShopDbContext db, string search, int take, int skip, string sort, string dir)
    {
        var predicate = PredicateBuilder.New<Product>(true);

        if (!string.IsNullOrWhiteSpace(search))
            predicate = predicate.And(i => i.Name!.ToLower().Contains(search.ToLower()));

        return new() {
            AdminProducts = await db.Products!.AsNoTracking()
            .Where(predicate)
            .Select(o => new AdminProductVm { Id = o.Id, Name = o.Name/*, StatusId = o.StatusId */})
            .OrderBy($"{sort} {dir}")
            .Skip(skip)
            .Take(take).ToListAsync(),

            FilteredCount = db.Products!.Where(predicate).Count(),
            TotalCount = db.Products!.Where(predicate).Count()
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

        return new DataTablesParameters
        {
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