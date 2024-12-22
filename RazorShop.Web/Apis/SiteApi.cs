using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Models;
using RazorShop.Web.Models.ViewModels;
using System.Text.Json;

namespace RazorShop.Web.Apis;

public static class SiteApis
{
    public static void SiteApi(this WebApplication app)
    {
        app.MapGet("/", async (RazorShopDbContext db) =>
        {
            ProductsVm vm = new();
            vm.Products = await db.Products!
                .AsNoTracking()
                .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
                .ToListAsync();

            return Results.Extensions.RazorSlice<Pages.Home, ProductsVm>(vm);
        });

        app.MapGet("/Redirects", (int statusCode) =>
        {

            if (statusCode == 404)
            {

            }

            return Results.Extensions.RazorSlice<Pages.NotFound>();
        });

        app.MapGet("/Error", (HttpContext context) =>
        {
            if (ApiUtil.IsHtmx(context.Request))
            {
                context.Response.Headers["HX-Target"] = "body";

                return Results.Extensions.RazorSlice<Pages.Error>();
            }

            // Access exception details, if needed
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            // Log the exception or handle it as needed
            Console.WriteLine($"Unhandled exception: {exception?.Message}");

            // Return a custom response or redirect
            //return Results.Problem(
            //    detail: "An unexpected error occurred.",
            //    statusCode: 500
            //);

            return Results.Extensions.RazorSlice<Pages.Error>();
        });
    }
}