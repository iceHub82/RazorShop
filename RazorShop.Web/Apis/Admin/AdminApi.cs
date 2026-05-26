using System.Security.Claims;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Identity;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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
                return Results.RazorSlice<Home>();
            }

            return Results.RazorSlice<Pages.Admin.Home>();
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/admin/products", (HttpContext http) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.RazorSlice<Products>();
            }

            return Results.RazorSlice<Pages.Admin.Products>();
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/admin/products/table", async (RazorShopDbContext db, HttpRequest request) =>
        {
            var dtParams = GetDatatableParameters(request, ProductSortColumns, "Id");

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
        }).RequireAuthorization("AdminOnly");

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
            vm.StatusId = product.StatusId;

            var token1 = antiforgery.GetAndStoreTokens(http);
            vm!.AdminProductFormAntiForgeryToken = token1.RequestToken;
            var token2 = antiforgery.GetAndStoreTokens(http);
            vm!.AdminProductFormMainImageAntiForgeryToken = token2.RequestToken;

            var categories = (IEnumerable<Category>)cache.Get("categories")!;
            foreach (var category in categories)
                vm.AdminCategories!.Add(new AdminCategoryVm { Id = category.Id, Name = category.Name });

            var statuses = (IEnumerable<Status>)cache.Get("statuses")!;
            foreach (var status in statuses)
                vm.AdminStatuses!.Add(new AdminStatusVm { Id = status.Id, Name = status.Name });

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

            return Results.RazorSlice<ProductEdit, AdminProductVm>(vm);
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/product/modal/edit/{id}", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var product = await db.Products!.FindAsync(id);

            var form = await http.Request.ReadFormAsync();

            product!.Name = form["name"];
            product.ShortDescription = form["shortDescription"];
            product.Description = form["description"];
            if (!int.TryParse(form["categoryDd"], out var categoryId) || !int.TryParse(form["statusDd"], out var statusId))
                return Results.BadRequest();
            product.CategoryId = categoryId == 0 ? null : categoryId;
            product.StatusId = statusId;

            var pSizes = await db.ProductSizes!.Where(ps => ps.ProductId == product.Id).ToListAsync();
            db.ProductSizes!.RemoveRange(pSizes);
            await db.SaveChangesAsync();

            var sizes = form["selectedSizes"];
            if (sizes.Count > 0)
                foreach (var sizeId in sizes)
                {
                    if (!int.TryParse(sizeId, out var parsedSizeId))
                        return Results.BadRequest();
                    await db.ProductSizes!.AddAsync(new ProductSize { ProductId = product.Id, SizeId = parsedSizeId });
                }

            if (form.TryGetValue("price", out var priceValue) && decimal.TryParse(priceValue, out var price))
                product.Price = price;
            else
                product.Price = 0m;

            await db.SaveChangesAsync();

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/admin/product/modal/new", (HttpContext http, IMemoryCache cache, IAntiforgery antiforgery) =>
        {
            var vm = new AdminNewProductVm();
            var token = antiforgery.GetAndStoreTokens(http);
            vm.AdminNewProductFormAntiForgeryToken = token.RequestToken;

            var categories = (IEnumerable<Category>)cache.Get("categories")!;
            foreach (var category in categories)
                vm.AdminCategories!.Add(new AdminCategoryVm { Id = category.Id, Name = category.Name });

            var sizes = (IEnumerable<Data.Entities.Size>)cache.Get("sizes")!;
            foreach (var size in sizes)
                vm.AdminSizes!.Add(new AdminSizeVm { Id = size.Id, Name = size.Name });

            return Results.RazorSlice<ProductNew, AdminNewProductVm>(vm);
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/product/modal/new", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            var form = await http.Request.ReadFormAsync();

            var product = new Product();
            product.Name = form["name"];
            product.ShortDescription = form["shortDescription"];
            product.Description = form["description"];
            if (!int.TryParse(form["categoryDd"], out var categoryId))
                return Results.BadRequest();
            product.CategoryId = categoryId == 0 ? null : categoryId;
            product.StatusId = 1;

            if (form.TryGetValue("price", out var priceValue) && decimal.TryParse(priceValue, out var price))
                product.Price = price;
            else
                product.Price = 0m;

            await db.Products!.AddAsync(product);
            await db.SaveChangesAsync();

            var sizes = form["selectedSizes"];
            if (sizes.Count > 0)
            {
                foreach (var sizeId in sizes)
                {
                    if (!int.TryParse(sizeId, out var parsedSizeId))
                        return Results.BadRequest();
                    await db.ProductSizes!.AddAsync(new ProductSize { ProductId = product.Id, SizeId = parsedSizeId });
                }

                await db.SaveChangesAsync();
            }
                
            return Results.Ok(new { product.Id });
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/product/upload-main/{id}", async (IWebHostEnvironment env, HttpContext http, RazorShopDbContext db, IFormFile img, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            if (!IsAllowedImageContentType(img.ContentType))
                return Results.BadRequest("Unsupported image type");

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
        }).RequireAuthorization("AdminOnly");

        app.MapPost("/admin/product/upload-images/{id}", async (IWebHostEnvironment env, HttpContext http, RazorShopDbContext db, IFormFileCollection files, IAntiforgery antiforgery, int id) =>
        {
            await antiforgery.ValidateRequestAsync(http);

            foreach (var f in files)
                if (!IsAllowedImageContentType(f.ContentType))
                    return Results.BadRequest("Unsupported image type");

            var uploadPath = $"{env.WebRootPath}\\products\\{id}\\gallery";

            var sizes = new List<(string type, int width, int height, int quality)> {
                ("thumbnail", 80, 100, 65),
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

            }

            return Results.Ok();
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/Login", (HttpContext http, IAntiforgery antiforgery) =>
        {
            var token = antiforgery.GetAndStoreTokens(http);
            var vm = new AdminLoginVm { AntiForgeryToken = token.RequestToken };
            return Results.RazorSlice<Pages.Admin.Login, AdminLoginVm>(vm);
        });

        app.MapPost("/Login", async (HttpContext context, IConfiguration config, IAntiforgery antiforgery, ILogger<object> log) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);

                var userName = context.Request.Form["username"];

                if (userName != config["AdminUser"])
                {
                    log.LogWarning("Login attempt with unknown username from {RemoteIp}", context.Connection.RemoteIpAddress);

                    return Results.Redirect("/Login");
                }

                var password = context.Request.Form["password"];

                var pHasher = new PasswordHasher<object>();
                var result = pHasher.VerifyHashedPassword(default!, config["AdminHash"]!, password!);

                if (result == PasswordVerificationResult.Failed)
                {
                    log.LogWarning("Failed password attempt for admin from {RemoteIp}", context.Connection.RemoteIpAddress);

                    return Results.Redirect("/Login");
                }

                var claims = new List<Claim> { new(ClaimTypes.Name, userName!), new(ClaimTypes.Role, "Admin") };

                var claimsIdentity = new ClaimsIdentity(claims, "App_Auth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await context.SignInAsync("App_Auth", claimsPrincipal);

                return Results.RazorSlice<Pages.Admin.Home>();
            }
            catch (AntiforgeryValidationException)
            {
                log.LogWarning("Login antiforgery validation failed");
                return Results.Redirect("/Login");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Login handler error");
                return Results.Redirect("/Login");
            }
        }).RequireRateLimiting("login");

        app.MapPost("/Logout", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await context.SignOutAsync("App_Auth");
            return Results.Redirect("/Login");
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/admin/orders", (HttpContext http) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.RazorSlice<Orders>();
            }

            return Results.RazorSlice<Pages.Admin.Orders>();
        }).RequireAuthorization("AdminOnly");

        app.MapGet("/admin/orders/table", async (RazorShopDbContext db, HttpRequest request) =>
        {
            var dtParams = GetDatatableParameters(request, OrderSortColumns, "Created");

            var vm = await GetPaginatedOrders(db, dtParams.Search!, dtParams.Take, dtParams.Skip, dtParams.Sort!, dtParams.SortDirection!);

            List<object> ordersTableVm = new();
            foreach (var order in vm.AdminOrders)
            {
                ordersTableVm.Add(new {
                    order.Id,
                    order.Reference,
                    order.Created
                });
            }

            return Results.Json(new
            {
                dtParams.Draw,
                recordsTotal = vm.TotalCount,
                recordsFiltered = vm.FilteredCount,
                data = ordersTableVm
            });
        }).RequireAuthorization("AdminOnly");
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

    private static async Task<AdminOrdersVm> GetPaginatedOrders(RazorShopDbContext db, string search, int take, int skip, string sort, string dir)
    {
        var predicate = PredicateBuilder.New<Order>(true);

        if (!string.IsNullOrWhiteSpace(search))
            predicate = predicate.And(i => i.Reference!.ToLower().Contains(search.ToLower()));

        return new()
        {
            AdminOrders = await db.Orders!.AsNoTracking()
            .Where(predicate)
            .Select(o => new AdminOrderVm { Id = o.Id, Reference = o.Reference, Created = o.Created.ToString("dd'-'MM'-'yyyy' 'HH':'mm':'ss")/*, StatusId = o.StatusId */})
            .OrderBy($"{sort} {dir}")
            .Skip(skip)
            .Take(take).ToListAsync(),

            FilteredCount = db.Orders!.Where(predicate).Count(),
            TotalCount = db.Orders!.Where(predicate).Count()
        };
    }

    private static readonly HashSet<string> ProductSortColumns = new(StringComparer.Ordinal) { "Id", "Name" };
    private static readonly HashSet<string> OrderSortColumns = new(StringComparer.Ordinal) { "Id", "Reference", "Created" };

    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    };

    private static bool IsAllowedImageContentType(string? contentType) =>
        !string.IsNullOrEmpty(contentType) && AllowedImageContentTypes.Contains(contentType);

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