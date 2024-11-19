using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Web.Models.ViewModels;
using RazorShop.Data;
using RazorShop.Data.Entities;

namespace RazorShop.Web.Apis;

public static class ShopCartApis
{
    public static void ShopCartApi(this WebApplication app)
    {
        app.MapGet("/ShoppingCart", async (HttpContext http, HttpRequest request, HttpResponse response, RazorShopDbContext db, IMemoryCache cache) =>
        {
            var cart = await GetCart(http, db);

            var items = await GetCartItems(cart.Id, db)!;

            var vm = GetShopCartViewModel(items, cache);

            if (ApiUtil.IsHtmx(request))
            {
                response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.ShopCart, ShopCartVm>(vm!);
            }

            return Results.Extensions.RazorSlice<Pages.ShopCart, ShopCartVm>(vm!);
        });
    }

    private static async Task<Cart> GetCart(HttpContext http, RazorShopDbContext db)
    {
        Cart? cart;

        if (!http.Request.Cookies.TryGetValue("CartSessionId", out var cartSessionGuid))
        {
            var guid = Guid.NewGuid();
            cartSessionGuid = guid.ToString();
            http.Response.Cookies.Append("CartSessionId", cartSessionGuid);

            cart = new Cart { CartGuid = guid, Created = DateTime.UtcNow };
            db.Carts!.Add(cart);
            await db.SaveChangesAsync();
        }
        else
            cart = await db.Carts!.Where(c => c.CartGuid == Guid.Parse(cartSessionGuid!)).FirstAsync();

        return cart;
    }

    private static async Task<List<CartItem>>? GetCartItems(int cartId, RazorShopDbContext db)
    {
        return await db.CartItems!.Where(c => c.CartId == cartId && !c.Deleted).Include(c => c.Product).ToListAsync();
    }

    private static ShopCartVm? GetShopCartViewModel(List<CartItem> items, IMemoryCache cache)
    {
        if (items.Count == 0)
            return new ShopCartVm();

        var sizes = (IEnumerable<Size>)cache.Get("sizes")!;

        return new ShopCartVm {
            CartQuantity = items.Sum(c => c.Quantity),
            ShopCartItems = items.Select(item => new ShopCartItemVm{
                Id = item.Id,
                Name = item.Product!.Name,
                Description = item.Product.Description,
                Price = $"{item.Product.Price:#.00} kr",
                Size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name,
                Quantity = item.Quantity
            }).ToList(),
            Total = $"{items.Sum(c => c.Product!.Price * c.Quantity):#.00} kr"
        };
    }
}