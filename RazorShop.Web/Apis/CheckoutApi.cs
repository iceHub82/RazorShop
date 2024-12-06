using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Models.ViewModels;

namespace RazorShop.Web.Apis;

public static class CheckoutApis
{
    public static void CheckoutApi(this WebApplication app)
    {
        app.MapGet("/checkout", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, IAntiforgery antiforgery) =>
        {
            var cart = await GetCart(http, db);

            var items = await GetCartItems(cart.Id, db)!;
            if (items.Count == 0)
                return Results.Extensions.RazorSlice<Pages.EmptyCart>();

            var vm = GetCheckoutViewModel(items, cache);

            var tokens = antiforgery.GetAndStoreTokens(http);
            vm!.CheckoutFormAntiForgeryToken = tokens.RequestToken;

            return Results.Extensions.RazorSlice<Pages.Checkout, CheckoutVm>(vm!);
        });

        app.MapGet("/checkout/update/{itemId}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, int itemId, int quantity) =>
        {
            var result = await UpdateCartItemQuantity(db, itemId, quantity);

            var cart = await GetCart(http, db);
            var items = await GetCartItems(cart.Id, db)!;

            var vm = GetCheckoutViewModel(items, cache);

            return Results.Extensions.RazorSlice<Slices.CheckoutUpdate, CheckoutVm>(vm!);
        });

        app.MapDelete("/checkout/delete/{id}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, int id) =>
        {
            var cart = await GetCart(http, db);

            var item = db.CartItems!.Find(id);
            item!.Deleted = true;
            item!.Updated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var items = await GetCartItems(cart.Id, db)!;
            if (items.Count == 0)
                return Results.Extensions.RazorSlice<Slices.EmptyCart>();

            var vm = GetCheckoutViewModel(items, cache);

            return Results.Extensions.RazorSlice<Slices.CheckoutUpdate, CheckoutVm>(vm!);
        });

        app.MapGet("/checkout/billing-address", (string? billingAddressCb) =>
        {
            if (billingAddressCb == "on")
                return Results.Extensions.RazorSlice<Slices.BillingAddress>();

            return Results.Content(string.Empty);
        });

        app.MapPost("/checkout/submit", async (HttpContext http, HttpRequest request, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(http); // Validate token

            var body = request.Body;

            var formData = await http.Request.ReadFormAsync();
            var name = formData["name"];
            var email = formData["email"];

            // Simulate saving data or processing
            return Results.Json(new { success = true, name, email });

            return Results.Content(string.Empty);
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
            cart = await db.Carts!.Where(c => c.CartGuid == Guid.Parse(cartSessionGuid!)).FirstOrDefaultAsync();

        return cart!;
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

    private static CheckoutVm? GetCheckoutViewModel(List<CartItem> items, IMemoryCache cache)
    {
        if (items.Count == 0)
            return new CheckoutVm();

        var sizes = (IEnumerable<Size>)cache.Get("sizes")!;

        return new CheckoutVm {
            CheckoutQuantity = items.Sum(c => c.Quantity),
            CheckoutItems = items.Select(item => new CheckoutItemVm {
                Id = item.Id,
                ProductId = item.ProductId,
                Name = item.Product!.Name,
                Description = item.Product.Description,
                Price = $"{item.Product.Price:#.00} kr",
                Size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name,
                Quantity = item.Quantity
            }).ToList(),
            CheckoutTotal = $"{items.Sum(c => c.Product!.Price * c.Quantity):#.00} kr"
        };
    }
}