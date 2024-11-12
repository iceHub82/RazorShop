using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.ViewModels;

namespace RazorShop.Web;

public static class MinimalApis
{
    public static void MinimalApi(this WebApplication app)
    {
        app.MapGet("/", async (RazorShopDbContext dbCtx) => {

            var products = await dbCtx.Products
                .AsNoTracking()
                //.Where(x => x.SomeCondition)
                .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = p.Price.ToString() })
                .ToListAsync();

            return Results.Extensions.RazorSlice<Pages.Home, List<ProductVm>>(products);
        });

        app.MapGet("/Product/{id}", async (HttpRequest request, HttpResponse response, RazorShopDbContext dbCtx, int id) =>
        {
            var product = await dbCtx.Products
                .Include(x => x.ProductSizes!)
                .ThenInclude(x => x.Size).AsNoTracking().FirstAsync(p => p.Id == id);

            var productVm = new ProductVm { Id = product.Id, Name = product.Name, Price = product.Price.ToString() };

            if (product.ProductSizes!.Any())
            {
                productVm.CheckedSizeId = product.ProductSizes!.First().SizeId;

                foreach (var size in product.ProductSizes!)
                    productVm.ProductSizes!.Add(new ProductSizeVm { Id = size.SizeId, Name = size.Size!.Name });
            }

            if (IsHtmx(request))
            {
                response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.Product, ProductVm>(productVm);
            }

            return Results.Extensions.RazorSlice<Pages.Product, ProductVm>(productVm);
        });

        app.MapGet("/cart", async (HttpContext ctx, RazorShopDbContext dbCtx) => {

            var sessionId = ctx.Request.Cookies["CartSessionId"];
            var cart = dbCtx.Carts.Where(c => c.CartGuid == Guid.Parse(sessionId!)).First();
            var cartItemsCount = await dbCtx.CartItems.CountAsync(c => c.CartId == cart.Id);

            return Results.Extensions.RazorSlice<Slices.CartButton, int>(cartItemsCount);
        });

        app.MapGet("/add-to-cart/{id}", async (HttpContext context, RazorShopDbContext dbCtx, int id, int checkedSize) =>
        {
            var sessionId = context.Request.Cookies["CartSessionId"];

            var cart = dbCtx.Carts.Where(c => c.CartGuid == Guid.Parse(sessionId!)).First();

            await dbCtx.CartItems.AddAsync(new CartItem { CartId = cart.Id, ProductId = id, SizeId = checkedSize == 0 ? null : checkedSize });
            await dbCtx.SaveChangesAsync();

            var cartItemsCount = dbCtx.CartItems.Count(c => c.CartId == cart.Id);

            return Results.Extensions.RazorSlice<Slices.CartButton, int>(cartItemsCount);
        });

        app.MapGet("/Redirects", (int statusCode) => {

            if (statusCode == 404)
            {

            }

            return Results.Extensions.RazorSlice<Slices.NotFound>();
        });

        app.MapGet("/Error", (HttpContext context) =>
        {
            // Access exception details, if needed
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            // Log the exception or handle it as needed
            Console.WriteLine($"Unhandled exception: {exception?.Message}");

            // Return a custom response or redirect
            //return Results.Problem(
            //    detail: "An unexpected error occurred.",
            //    statusCode: 500
            //);

            return Results.Extensions.RazorSlice<Slices.Error>();
        });
    }

    static bool IsHtmx(HttpRequest request)
    {
        return request.Headers["hx-request"] == "true";
    }
}