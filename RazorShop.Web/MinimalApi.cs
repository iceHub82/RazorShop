using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Web.Models.ViewModels;
using RazorShop.Data;
using RazorShop.Data.Entities;

namespace RazorShop.Web;

public static class MinimalApis
{
    public static void MinimalApi(this WebApplication app)
    {
        app.MapGet("/", async (RazorShopDbContext dbCtx) => {

            var products = await dbCtx.Products!
                .AsNoTracking()
                //.Where(x => x.SomeCondition)
                .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = p.Price.ToString() })
                .ToListAsync();

            return Results.Extensions.RazorSlice<Pages.Home, List<ProductVm>>(products);
        });

        //app.MapGet("/Products", async (RazorShopDbContext dbCtx, HttpRequest request) =>
        //{
        //    var products = await dbCtx.Products
        //        .AsNoTracking()
        //        //.Where(x => x.SomeCondition)
        //        .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = p.Price.ToString() })
        //        .ToListAsync();

        //    return Results.Extensions.RazorSlice<Slices.Products, List<ProductVm>>(products);
        //});

        app.MapGet("/Product/{id}", async (HttpRequest request, HttpResponse response, RazorShopDbContext dbCtx, int id) =>
        {
            var product = await dbCtx.Products!
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

        app.MapGet("/cart", async (HttpContext ctx, RazorShopDbContext dbCtx, IMemoryCache cache) => {

            var sessionId = ctx.Request.Cookies["CartSessionId"];
            var cart = dbCtx.Carts!.Where(c => c.CartGuid == Guid.Parse(sessionId!)).FirstOrDefault();

            var cartVm = new CartVm();

            if (cart != null)
            {
                var cartItems = await dbCtx.CartItems!.Where(c => c.CartId == cart.Id && !c.Deleted).Include(c => c.Product).ToListAsync();

                cartVm.CartQuantity = cartItems.Sum(c => c.Quantity);

                foreach (var item in cartItems)
                {
                    var size = ((IEnumerable<Size>)cache.Get("sizes")!).Where(s => s.Id == item.SizeId).FirstOrDefault();
                    cartVm.CartItems!.Add(new CartItemVm { Id = item.Id, Name = item.Product!.Name, Price = $"{item.Product.Price:#.00} kr", Size = size?.Name, Quantity = item.Quantity });
                }

                var total = cartItems.Sum(c => c.Product!.Price * c.Quantity);
                cartVm.Total = $"{total:#.00} kr";
            }

            return Results.Extensions.RazorSlice<Slices.Cart, CartVm>(cartVm);
        });

        app.MapGet("/cart/add/{id}", async (HttpContext context, RazorShopDbContext dbCtx, IMemoryCache cache, int id, int checkedSize, int quantity) =>
        {
            var sessionId = context.Request.Cookies["CartSessionId"];
            var cart = dbCtx.Carts!.Where(c => c.CartGuid == Guid.Parse(sessionId!)).First();

            int? sizeId = checkedSize == 0 ? null : checkedSize;

            var existingCartItem = dbCtx.CartItems!.FirstOrDefault(c => c.CartId == cart.Id && c.ProductId == id && c.SizeId == sizeId);

            if (existingCartItem == null)
                await dbCtx.CartItems!.AddAsync(new CartItem { CartId = cart.Id, ProductId = id, SizeId = sizeId, Quantity = quantity, Created = DateTime.UtcNow });
            else
            {
                existingCartItem.Quantity += quantity;
                existingCartItem.Updated = DateTime.UtcNow;
            }

            await dbCtx.SaveChangesAsync();

            var cartItems = await dbCtx.CartItems!.Where(c => c.CartId == cart.Id && !c.Deleted).Include(c => c.Product).ToListAsync();

            var cartVm = new CartVm();
            cartVm.CartQuantity = cartItems.Sum(c => c.Quantity);

            foreach (var item in cartItems)
            {
                var size = ((IEnumerable<Size>)cache.Get("sizes")!).Where(s => s.Id == item.SizeId).FirstOrDefault();
                cartVm.CartItems!.Add(new CartItemVm { Id = item.Id, Name = item.Product!.Name, Price = $"{item.Product.Price:#.00} kr", Size = size?.Name, Quantity = item.Quantity });
            }

            var total = cartItems.Sum(c => c.Product!.Price * c.Quantity);
            cartVm.Total = $"{total:#.00} kr";

            return Results.Extensions.RazorSlice<Slices.Cart, CartVm>(cartVm);
        });

        app.MapDelete("/cart/delete/{id}", async (HttpContext context, RazorShopDbContext dbCtx, IMemoryCache cache, int id) =>
        {
            var sessionId = context.Request.Cookies["CartSessionId"];
            var cart = dbCtx.Carts!.Where(c => c.CartGuid == Guid.Parse(sessionId!)).First();

            var cartItem = dbCtx.CartItems!.Find(id);
            cartItem!.Deleted = true;
            cartItem!.Updated = DateTime.UtcNow;
            await dbCtx.SaveChangesAsync();

            var cartItems = await dbCtx.CartItems.Where(c => c.CartId == cart.Id && !c.Deleted).Include(c => c.Product).ToListAsync();

            var cartVm = new CartVm();
            cartVm.CartQuantity = cartItems.Sum(c => c.Quantity);

            foreach (var item in cartItems)
            {
                var size = ((IEnumerable<Size>)cache.Get("sizes")!).Where(s => s.Id == item.SizeId).FirstOrDefault();
                cartVm.CartItems!.Add(new CartItemVm { Id = item.Id, Name = item.Product!.Name, Price = $"{item.Product.Price:#.00} kr", Size = size?.Name, Quantity = item.Quantity });
            }

            var total = cartItems.Sum(c => c.Product!.Price * c.Quantity);
            cartVm.Total = $"{total:#.00} kr";

            return Results.Extensions.RazorSlice<Slices.Cart, CartVm>(cartVm);
        });

        app.MapGet("/cart/list", async (HttpContext context, RazorShopDbContext dbCtx, IMemoryCache cache) =>
        {
            var sessionId = context.Request.Cookies["CartSessionId"];
            var cart = dbCtx.Carts!.Where(c => c.CartGuid == Guid.Parse(sessionId!)).First();
            var cartItems = await dbCtx.CartItems!.Where(c => c.CartId == cart.Id && c.Deleted!).Include(c => c.Product).ToListAsync();

            var cartVm = new ShopCartVm();
            cartVm.CartItemsCount = cartItems.Count;

            foreach (var item in cartItems)
            {
                var size = ((IEnumerable<Size>)cache.Get("sizes")!).Where(s => s.Id == item.SizeId).FirstOrDefault();
                cartVm.CartItems!.Add(new CartItemVm { Id = item.Id, Name = item.Product!.Name, Price = $"{item.Product.Price:#.00} kr", Size = size?.Name });
            }

            var total = cartItems.Sum(c => c.Product!.Price);
            cartVm.Total = $"{total:#.00} kr";

            return Results.Extensions.RazorSlice<Slices.ShopCart, ShopCartVm>(cartVm);
        });

        app.MapGet("/Redirects", (int statusCode) => {

            if (statusCode == 404)
            {

            }

            return Results.Extensions.RazorSlice<Pages.NotFound>();
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

            return Results.Extensions.RazorSlice<Pages.Error>();
        });
    }

    static bool IsHtmx(HttpRequest request)
    {
        return request.Headers["hx-request"] == "true";
    }
}