using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Models.ViewModels;

namespace RazorShop.Web.Apis;

public static class CheckoutCartApis
{
    public static void CheckoutCartApi(this WebApplication app)
    {
        app.MapGet("/Cart", async (HttpContext http, HttpRequest request, HttpResponse response, RazorShopDbContext db, IMemoryCache cache) =>
        {
            var cart = await GetCart(http, db);

            var items = await GetCartItems(cart.Id, db)!;

            var vm = GetCheckoutCartViewModel(items, cache);

            if (ApiUtil.IsHtmx(request))
            {
                response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Slices.CheckoutCart, CheckoutCartVm>(vm!);
            }

            return Results.Extensions.RazorSlice<Pages.CheckoutCart, CheckoutCartVm>(vm!);
        });

        app.MapGet("/cart/updatecartitemquantity/{itemId}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, int itemId, int quantity) =>
        {
            await UpdateCartItemQuantity(db, itemId, quantity);

            var cart = await GetCart(http, db);

            var items = await GetCartItems(cart.Id, db)!;

            var vm = GetCheckoutCartViewModel(items, cache);

            return Results.Extensions.RazorSlice<Slices.CartUpdate, CheckoutCartVm>(vm!);
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

    private static async Task<bool> UpdateCartItemQuantity(RazorShopDbContext db, int itemId, int quantity)
    {
        var item = db.CartItems!.Find(itemId);
        item!.Quantity = quantity;
        item!.Updated = DateTime.UtcNow;

        return await db.SaveChangesAsync() > 0;
    }

    private static async Task<List<CartItem>>? GetCartItems(int cartId, RazorShopDbContext db)
    {
        return await db.CartItems!.Where(c => c.CartId == cartId && !c.Deleted).Include(c => c.Product).ToListAsync();
    }

    private static CheckoutCartVm? GetCheckoutCartViewModel(List<CartItem> items, IMemoryCache cache)
    {
        if (items.Count == 0)
            return new CheckoutCartVm();

        var sizes = (IEnumerable<Size>)cache.Get("sizes")!;

        return new CheckoutCartVm {
            CheckoutCartQuantity = items.Sum(c => c.Quantity),
            CheckoutCartItems = items.Select(item => new CheckoutCartItemVm{
                Id = item.Id,
                Name = item.Product!.Name,
                Description = item.Product.Description,
                Price = $"{item.Product.Price:#.00} kr",
                Size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name,
                Quantity = item.Quantity
            }).ToList(),
            CheckoutCartTotal = $"{items.Sum(c => c.Product!.Price * c.Quantity):#.00} kr"
        };
    }
}