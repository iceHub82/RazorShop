using Microsoft.EntityFrameworkCore;
using RazorShop.Data;
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
                return Results.Extensions.RazorSlice<Slices.Products, ProductsVm>(vm);
            }

            return Results.Extensions.RazorSlice<Pages.Products, ProductsVm>(vm);
        });

        app.MapGet("/Products/{category}", async (RazorShopDbContext db, HttpContext http, ImagesRepo imgRepo, string category) =>
        {
            ProductsVm vm = new();
            vm.Products = await GetProductsByCategoryName(db, imgRepo, category);
            vm.Category = category;

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.Products, ProductsVm>(vm);
            }

            return Results.Extensions.RazorSlice<Pages.Products, ProductsVm>(vm);
        });

        app.MapGet("/Product/{id}", async (RazorShopDbContext db, HttpContext http, ImagesRepo imgRepo, int id) =>
        {
            var product = await db.Products!
                .Include(x => x.ProductSizes!)
                .ThenInclude(x => x.Size)
                .Include(x => x.ProductImages!)
                .ThenInclude(x => x.Image)
                .AsNoTracking().FirstAsync(p => p.Id == id);

            var vm = new ProductVm { Id = product.Id, Name = product.Name, Price = $"{product.Price:#.00} kr", Description = product.Description };

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

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.Product, ProductVm>(vm);
            }

            return Results.Extensions.RazorSlice<Pages.Product, ProductVm>(vm);
        });
    }

    private static async Task<List<ProductVm>> GetProducts(RazorShopDbContext db, ImagesRepo imgRepo)
    {
        var products =  await db.Products!
            .AsNoTracking()
            .Where(p => p.StatusId == 2)
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
            .Where(p => p.StatusId == 2 && p.Category!.Name == name)
            .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
            .ToListAsync();

        foreach (var product in products)
            product.TicksStamp = await imgRepo.GetMainProductImageTickStamp(product.Id);

        return products!;
    }
}