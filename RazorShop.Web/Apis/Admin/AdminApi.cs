using System.Security.Claims;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Repos;
using RazorShop.Data.Entities;
using RazorShop.Web.Slices.Admin;
using RazorShop.Web.Models.ViewModels;
using LinqKit;
using SixLabors.ImageSharp.Formats.Webp;

using Size = SixLabors.ImageSharp.Size;
using Image = SixLabors.ImageSharp.Image;

namespace RazorShop.Web.Apis.Admin;

public static class AdminApis
{
    public static void AdminApi(this WebApplication app)
    {
        app.MapGet("/admin", (HttpContext http) =>
        {

            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Home>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Home>();
        }).RequireAuthorization();

        app.MapGet("/admin/orders", (HttpContext http) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Orders>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Orders>();
        }).RequireAuthorization();

        app.MapGet("/admin/products", (HttpContext http) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Products>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Products>();
        }).RequireAuthorization();

        app.MapGet("/admin/products/table", async (RazorShopDbContext db, HttpRequest request) =>
        {
            var dtParams = GetDatatableParameters(request);

            var vm = await GetPaginatedAdminProducts(db, dtParams.Search!, dtParams.Take, dtParams.Skip, dtParams.Sort!, dtParams.SortDirection!);

            List<object> productTableVm = new();
            foreach (var product in vm.AdminProducts)
            {
                productTableVm.Add(new
                {
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

        app.MapGet("/admin/product/modal/edit/{id}", async (HttpContext http, ImagesRepo imgRepo, IAntiforgery antiforgery, RazorShopDbContext db, IMemoryCache cache, int id) =>
        {
            var product = await db.Products!.Where(p => p.Id == id).Include(p => p.ProductSizes)!.ThenInclude(p => p.Size).Include(x => x.ProductImages)!.ThenInclude(x => x.Image).FirstAsync();

            var vm = new AdminProductVm();
            vm.Id = product.Id;
            vm.Name = product.Name;
            vm.Price = $"{product.Price:#.00}";
            vm.Description = product.Description;
            vm.ShortDescription = product.ShortDescription;
            vm.TicksStamp = await imgRepo.GetMainProductImageTickStamp(id);
            vm.CategoryId = product.CategoryId;

            var token1 = antiforgery.GetAndStoreTokens(http);
            vm!.AdminProductFormAntiForgeryToken = token1.RequestToken;
            var token2 = antiforgery.GetAndStoreTokens(http);
            vm!.AdminProductFormMainImageAntiForgeryToken = token2.RequestToken;

            var categories = (IEnumerable<Category>)cache.Get("categories")!;
            foreach (var category in categories)
                vm.AdminCategories!.Add(new AdminCategoryVm { Id = category.Id, Name = category.Name });

            var sizes = (IEnumerable<Data.Entities.Size>)cache.Get("sizes")!;
            var pSizes = db.ProductSizes!.Where(ps => ps.ProductId == product.Id);

            foreach (var size in sizes)
            {
                var adminSize = new AdminSizeVm { Id = size.Id, Name = size.Name };

                foreach (var pSize in pSizes)
                    if (pSize.SizeId == size.Id)
                        adminSize.Selected = true;

                vm.AdminSizes!.Add(adminSize);
            }

            var imgIds = product.ProductImages!.Where(x => x.ProductId == id && !x.Image!.Main).Select(x => x.ImageId);
            foreach (var imgId in imgIds)
                vm.AdminImageVms!.Add(new AdminImageVm { Id = imgId, TicksStamp = await imgRepo.GetGalleryProductImageTickStamp(imgId) });

            return Results.Extensions.RazorSlice<ProductEdit, AdminProductVm>(vm);
        }).RequireAuthorization();

        app.MapPost("/admin/product/modal/edit/{id}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var product = await db.Products!.FindAsync(id);

            var form = await http.Request.ReadFormAsync();

            product!.Name = form["name"];
            product.ShortDescription = form["shortDescription"];
            product.Description = form["description"];
            var categoryId = int.Parse(form["categoryDd"]!);
            product.CategoryId = categoryId == 0 ? null : categoryId;

            var pSizes = await db.ProductSizes!.Where(ps => ps.ProductId == product.Id).ToListAsync();
            db.ProductSizes!.RemoveRange(pSizes);
            await db.SaveChangesAsync();

            var sizes = form["selectedSizes"];
            if (sizes.Count != 0)
                foreach (var sizeId in sizes)
                    await db.ProductSizes!.AddAsync(new ProductSize { ProductId = product.Id, SizeId = int.Parse(sizeId!) });

            if (form.TryGetValue("price", out var priceValue) && decimal.TryParse(priceValue, out var price))
                product.Price = price;
            else
                product.Price = 0m;

            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization();

        app.MapGet("/admin/product/modal/new", (HttpContext http, IMemoryCache cache, IAntiforgery antiforgery) =>
        {
            var vm = new AdminNewProductVm();
            var token = antiforgery.GetAndStoreTokens(http);
            vm.AdminNewProductFormAntiForgeryToken = token.RequestToken;

            var categories = (IEnumerable<Category>)cache.Get("categories")!;
            foreach (var category in categories)
                vm.AdminCategories!.Add(new AdminCategoryVm { Id = category.Id, Name = category.Name });

            return Results.Extensions.RazorSlice<ProductNew, AdminNewProductVm>(vm);
        }).RequireAuthorization();

        app.MapPost("/admin/product/modal/new", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            var product = new Product();
            product.Name = form["name"];
            product.ShortDescription = form["shortDescription"];
            product.Description = form["description"];
            var categoryId = int.Parse(form["categoryDd"]!);
            product.CategoryId = categoryId == 0 ? null : categoryId;

            if (form.TryGetValue("price", out var priceValue) && decimal.TryParse(priceValue, out var price))
                product.Price = price;
            else
                product.Price = 0m;

            await db.Products!.AddAsync(product);
            await db.SaveChangesAsync();

            return Results.Content($"TESTTEST");
        }).RequireAuthorization();

        app.MapPost("/admin/product/upload-main/{id}", async (IWebHostEnvironment env, HttpContext http, RazorShopDbContext db, IFormFile img, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var imgTimeStamp = DateTime.UtcNow;

            var uploadPath = $"{env.WebRootPath}\\products\\{id}\\main";
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
                await db.ProductImages!.AddAsync(new ProductImage { Image = new Data.Entities.Image { ContentType = img.ContentType, Main = true, FileName = img.FileName, Created = imgTimeStamp }, ProductId = id });
                await db.SaveChangesAsync();
            }
            else
            {
                var prodImg = await db.ProductImages!.Include(x => x.Image).FirstAsync(x => x.ProductId == id && x.Image!.Main);
                prodImg.Image!.FileName = img.FileName;
                prodImg.Image!.ContentType = img.ContentType;
                prodImg.Image!.Updated = imgTimeStamp;
                await db.SaveChangesAsync();

                foreach (var file in Directory.GetFiles(uploadPath))
                    File.Delete(file);
                foreach (var subDir in Directory.GetDirectories(uploadPath))
                    Directory.Delete(subDir, true);
            }

            var sizes = new List<(string type, int width, int height, int quality)> {
                ("thumbnail", 80, 100, 65),
                ("listing", 260, 330, 75),
                ("product", 600, 740, 80),
                //("zoom", 1024, 1200)
            };

            foreach (var (type, width, height, quality) in sizes)
            {
                var outputPath = Path.Combine(uploadPath, $"{id}_{type}.webp");

                var stream = img.OpenReadStream();

                using (var image = await Image.LoadAsync(stream))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Crop
                    }));

                    await image.SaveAsync(outputPath, new WebpEncoder { Quality = quality });
                }

                stream.Position = 0;
            }

            return Results.Content($"<img src='/products/{id}/main/{id}_thumbnail.webp?v={imgTimeStamp.Ticks}'");
        }).RequireAuthorization();

        app.MapPost("/admin/product/upload-images/{id}", async (IWebHostEnvironment env, HttpContext http, RazorShopDbContext db, IFormFileCollection files, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var uploadPath = $"{env.WebRootPath}\\products\\{id}\\gallery";

            var sizes = new List<(string type, int width, int height, int quality)> {
                ("thumbnail", 80, 100, 65),
                //("listing", 260, 330, 75),
                ("product", 600, 740, 80),
                //("zoom", 1024, 1200)
            };

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            foreach (var file in files)
            {
                var timeStamp = DateTime.UtcNow;

                var prodImg = new ProductImage { Image = new Data.Entities.Image { ContentType = file.ContentType, FileName = file.FileName, Created = timeStamp }, ProductId = id };
                await db.ProductImages!.AddAsync(prodImg);
                await db.SaveChangesAsync();

                foreach (var (type, width, height, quality) in sizes)
                {
                    var outputPath = Path.Combine(uploadPath, $"{prodImg.ImageId}_{type}.webp");

                    var stream = file.OpenReadStream();

                    using (var image = await Image.LoadAsync(stream))
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Mode = ResizeMode.Crop
                        }));

                        await image.SaveAsync(outputPath, new WebpEncoder { Quality = quality });
                    }

                    stream.Position = 0;
                }

                //string test += $"<img src='/products/{id}/main/{id}_thumbnail.webp?v={imgTimeStamp.Ticks}'";
            }
        }).RequireAuthorization();

        app.MapGet("/Login", () =>
        {
            return Results.Extensions.RazorSlice<Pages.Admin.Login>();
        });

        app.MapPost("/Login", async (HttpContext context) =>
        {
            var username = context.Request.Form["username"];
            var password = context.Request.Form["password"];

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

        return new()
        {
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