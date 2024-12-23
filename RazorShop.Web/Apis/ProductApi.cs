﻿using Microsoft.EntityFrameworkCore;
using RazorShop.Data;
using RazorShop.Web.Models.ViewModels;

namespace RazorShop.Web.Apis;

public static class ProductApis
{
    public static void ProductApi(this WebApplication app)
    {
        app.MapGet("/Products", async (RazorShopDbContext db, HttpContext http) =>
        {
            ProductsVm vm = new();
            vm.Products = await GetProducts(db);

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.Products, ProductsVm>(vm);
            }

            return Results.Extensions.RazorSlice<Pages.Products, ProductsVm>(vm);
        });

        app.MapGet("/Products/{category}", async (RazorShopDbContext db, HttpContext http, string category) =>
        {
            ProductsVm vm = new();
            vm.Products = await GetProductsByCategoryName(db, category);
            vm.Category = category;

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.Products, ProductsVm>(vm);
            }

            return Results.Extensions.RazorSlice<Pages.Products, ProductsVm>(vm);
        });

        app.MapGet("/Product/{id}", async(RazorShopDbContext db, HttpContext http, int id) =>
        {
            var product = await db.Products!
                .Include(x => x.ProductSizes!)
                .ThenInclude(x => x.Size).AsNoTracking().FirstAsync(p => p.Id == id);

            var productVm = new ProductVm { Id = product.Id, Name = product.Name, Price = product.Price.ToString() };

            if (product.ProductSizes!.Any())
            {
                productVm.CheckedSizeId = product.ProductSizes!.First().SizeId;

                foreach (var size in product.ProductSizes!)
                    productVm.ProductSizes!.Add(new ProductSizeVm { Id = size.SizeId, Name = size.Size!.Name });
            }

            if (ApiUtil.IsHtmx(http.Request))
            {
                http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.Product, ProductVm>(productVm);
            }

            return Results.Extensions.RazorSlice<Pages.Product, ProductVm>(productVm);
        });
    }

    private static async Task<List<ProductVm>> GetProducts(RazorShopDbContext db)
    {
        return await db.Products!
                .AsNoTracking()
                .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
                .ToListAsync();
    }

    private static async Task<List<ProductVm>> GetProductsByCategoryName(RazorShopDbContext db, string name)
    {
        return await db.Products!
                .AsNoTracking()
                .Where(p => p.Category!.Name == name)
                .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
                .ToListAsync();
    }
}