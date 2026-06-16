using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Data.Repos;
using RazorShop.Web.Models.ViewModels;

namespace RazorShop.Web.Apis;

public static class ProductApis
{
    public static void ProductApi(this WebApplication app)
    {
        app.MapGet("/Products", async (RazorShopDbContext db, HttpContext http, ImagesRepo imgRepo) =>
        {
            ProductsVm vm = new();
            vm.Products = await GetProducts(db, imgRepo);

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.RazorSlice<Slices.Products, ProductsVm>(vm);
            }

            return Results.RazorSlice<Pages.Products, ProductsVm>(vm);
        });

        app.MapGet("/Products/{category}", async (RazorShopDbContext db, HttpContext http, ImagesRepo imgRepo, string category) =>
        {
            ProductsVm vm = new();
            vm.Products = await GetProductsByCategoryName(db, imgRepo, category);
            vm.Category = category;

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.RazorSlice<Slices.Products, ProductsVm>(vm);
            }

            return Results.RazorSlice<Pages.Products, ProductsVm>(vm);
        });

        app.MapGet("/Product/{id}", async (RazorShopDbContext db, HttpContext http, ImagesRepo imgRepo, IAntiforgery antiforgery, IConfiguration config, int id) =>
        {
            var product = await db.Products!
                .Include(x => x.Category)
                .Include(x => x.ProductSizes!)
                .ThenInclude(x => x.Size)
                .Include(x => x.ProductImages!)
                .ThenInclude(x => x.Image)
                .AsNoTracking().FirstAsync(p => p.Id == id);

            var vm = new ProductVm { Id = product.Id, Name = product.Name, Price = $"{product.Price:#.00} kr", Description = product.Description };

            var token = antiforgery.GetAndStoreTokens(http);
            vm.AntiForgeryToken = token.RequestToken;

            vm.TicksStamp = await imgRepo.GetMainProductImageTickStamp(product.Id);

            var imgIds = product.ProductImages!.Where(x => x.ProductId == id && !x.Image!.Main).Select(x => x.ImageId);

            foreach (var imgId in imgIds)
                vm.ProductImages!.Add(new ProductImageVm { Id = imgId, TicksStamp = await imgRepo.GetGalleryProductImageTickStamp(imgId) });

            if (product.ProductSizes!.Any())
            {
                vm.CheckedSizeId = product.ProductSizes!.First().SizeId;

                foreach (var size in product.ProductSizes!)
                    vm.ProductSizes!.Add(new ProductSizeVm { Id = size.SizeId, Name = size.Size!.Name });
            }

            vm.JsonLd = BuildProductJsonLd(product, http, config);

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.RazorSlice<Slices.Product, ProductVm>(vm);
            }

            return Results.RazorSlice<Pages.Product, ProductVm>(vm);
        });
    }

    internal static string BuildProductJsonLd(Data.Entities.Product product, HttpContext http, IConfiguration config)
    {
        var baseUrl = (config["Shop:Link"]?.TrimEnd('/'))
            ?? $"{http.Request.Scheme}://{http.Request.Host}";
        var shopName = config["Shop:Name"] ?? "Artform.dk";

        var productUrl = $"{baseUrl}/Product/{product.Id}";
        var imageUrl = $"{baseUrl}/products/{product.Id}/main/{product.Id}_product.webp";
        var availability = product.StatusId == (int)EntityStatus.Active
            ? "https://schema.org/InStock"
            : "https://schema.org/OutOfStock";

        var productSchema = new Dictionary<string, object?> {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = product.Name,
            ["description"] = product.Description,
            ["sku"] = product.Id.ToString(CultureInfo.InvariantCulture),
            ["image"] = new[] { imageUrl },
            ["brand"] = new Dictionary<string, object?> {
                ["@type"] = "Brand",
                ["name"] = shopName,
            },
            ["offers"] = new Dictionary<string, object?> {
                ["@type"] = "Offer",
                ["url"] = productUrl,
                ["priceCurrency"] = "DKK",
                ["price"] = product.Price.ToString("0.00", CultureInfo.InvariantCulture),
                ["availability"] = availability,
                ["seller"] = new Dictionary<string, object?> {
                    ["@type"] = "Organization",
                    ["name"] = shopName,
                },
            },
        };

        var breadcrumb = new Dictionary<string, object?> {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = new object[] {
                new Dictionary<string, object?> {
                    ["@type"] = "ListItem",
                    ["position"] = 1,
                    ["name"] = "Forside",
                    ["item"] = $"{baseUrl}/",
                },
                new Dictionary<string, object?> {
                    ["@type"] = "ListItem",
                    ["position"] = 2,
                    ["name"] = product.Category?.Name ?? "Produkter",
                    ["item"] = product.Category != null
                        ? $"{baseUrl}/Products/{Uri.EscapeDataString(product.Category.Name ?? string.Empty)}"
                        : $"{baseUrl}/Products",
                },
                new Dictionary<string, object?> {
                    ["@type"] = "ListItem",
                    ["position"] = 3,
                    ["name"] = product.Name,
                    ["item"] = productUrl,
                },
            },
        };

        return JsonSerializer.Serialize(new object[] { productSchema, breadcrumb });
    }

    private static async Task<List<ProductVm>> GetProducts(RazorShopDbContext db, ImagesRepo imgRepo)
    {
        var products =  await db.Products!
            .AsNoTracking()
            .Where(p => p.StatusId == (int)EntityStatus.Active)
            .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
            .ToListAsync();

        foreach (var product in products)
            product.TicksStamp = await imgRepo.GetMainProductImageTickStamp(product.Id);

        return products!;
    }

    private static async Task<List<ProductVm>> GetProductsByCategoryName(RazorShopDbContext db, ImagesRepo imgRepo, string name)
    {
        var products = await db.Products!
            .AsNoTracking()
            .Where(p => p.StatusId == (int)EntityStatus.Active && p.Category!.Name == name)
            .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
            .ToListAsync();

        foreach (var product in products)
            product.TicksStamp = await imgRepo.GetMainProductImageTickStamp(product.Id);

        return products!;
    }
}