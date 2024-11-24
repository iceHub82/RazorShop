using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Models.ViewModels;

namespace RazorShop.Web.Apis;

public static class CartApis
{
    public static void CartApi(this WebApplication app)
    {
        app.MapGet("/shoppingcart", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache) =>
        {
            var cart = await GetCart(http, db);
            var items = await GetCartItems(cart.Id, db)!;
            var vm = GetShoppingCartViewModel(items!, cache);

            return Results.Extensions.RazorSlice<Slices.ShoppingCart, ShoppingCartVm>(vm!);
        });

        app.MapGet("/shoppingcart/add/{id}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, int id, int size, int quantity) =>
        {
            var cart = await GetCart(http, db);

            int? sizeId = size == 0 ? null : size;

            var existingCartItem = db.CartItems!.FirstOrDefault(c => c.CartId == cart.Id && c.ProductId == id && c.SizeId == sizeId && !c.Deleted);

            if (existingCartItem == null)
                await db.CartItems!.AddAsync(new CartItem { CartId = cart.Id, ProductId = id, SizeId = sizeId, Quantity = quantity, Created = DateTime.UtcNow });
            else
            {
                existingCartItem.Quantity += quantity;
                existingCartItem.Updated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            var items = await GetCartItems(cart.Id, db)!;
            var vm = GetShoppingCartViewModel(items, cache);

            return Results.Extensions.RazorSlice<Slices.ShoppingCart, ShoppingCartVm>(vm!);
        });

        app.MapDelete("/shoppingcart/delete/{id}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, int id) =>
        {
            var cart = await GetCart(http, db);

            var item = db.CartItems!.Find(id);
            item!.Deleted = true;
            item!.Updated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var items = await GetCartItems(cart.Id, db)!;
            var shoppingCartVm = GetShoppingCartViewModel(items, cache);

            // We need to check if we are on the checkoutcart page to update both shopping cart
            // and checkout cart. Otherwise only update shopping cart
            if (http.Request.Headers.TryGetValue("Referer", out var referer)) {
                if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri)) {
                    if (refererUri.PathAndQuery == "/Cart")
                    {
                        var checkoutCartVm = GetCheckoutCartViewModel(items, cache);

                        var vm = new UpdateCheckoutCartVm();
                        vm.CheckoutCartVm = checkoutCartVm;
                        vm.ShoppingCartVm = shoppingCartVm;

                        return Results.Extensions.RazorSlice<Slices.CheckoutCartUpdate, UpdateCheckoutCartVm>(vm!);
                    }
                }
            }

            return Results.Extensions.RazorSlice<Slices.ShoppingCart, ShoppingCartVm>(shoppingCartVm!);
        });

        app.MapDelete("/checkoutcart/delete/{id}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, int id) =>
        {
            var cart = await GetCart(http, db);

            var item = db.CartItems!.Find(id);
            item!.Deleted = true;
            item!.Updated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var items = await GetCartItems(cart.Id, db)!;
            var shoppingCartVm = GetShoppingCartViewModel(items, cache);
            var checkoutCartVm = GetCheckoutCartViewModel(items, cache);

            var vm = new UpdateCheckoutCartVm();
            vm.CheckoutCartVm = checkoutCartVm;
            vm.ShoppingCartVm = shoppingCartVm;

            return Results.Extensions.RazorSlice<Slices.CheckoutCartUpdate, UpdateCheckoutCartVm>(vm!);
        });

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
            var result = await UpdateCartItemQuantity(db, itemId, quantity);

            var cart = await GetCart(http, db);

            var items = await GetCartItems(cart.Id, db)!;

            var shoppingCartVm = GetShoppingCartViewModel(items, cache);
            var checkoutCartVm = GetCheckoutCartViewModel(items, cache);

            var vm = new UpdateCheckoutCartVm();
            vm.ShoppingCartVm = shoppingCartVm;
            vm.CheckoutCartVm = checkoutCartVm;

            return Results.Extensions.RazorSlice<Slices.CheckoutCartUpdate, UpdateCheckoutCartVm>(vm!);
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

    private static ShoppingCartVm? GetShoppingCartViewModel(List<CartItem> items, IMemoryCache cache)
    {
        if (items.Count == 0)
            return new ShoppingCartVm();

        var sizes = (IEnumerable<Size>)cache.Get("sizes")!;

        return new ShoppingCartVm {
            ShoppingCartQuantity = items.Sum(c => c.Quantity),
            ShoppingCartItems = items.Select(item => new ShoppingCartItemVm {
                Id = item.Id,
                Name = item.Product!.Name,
                Description = item.Product.Description,
                Price = $"{item.Product.Price:#.00} kr",
                Size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name,
                Quantity = item.Quantity
            }).ToList(),
            ShoppingCartTotal = $"{items.Sum(c => c.Product!.Price * c.Quantity):#.00} kr"
        };
    }

    private static CheckoutCartVm? GetCheckoutCartViewModel(List<CartItem> items, IMemoryCache cache)
    {
        if (items.Count == 0)
            return new CheckoutCartVm();

        var sizes = (IEnumerable<Size>)cache.Get("sizes")!;

        return new CheckoutCartVm
        {
            CheckoutCartQuantity = items.Sum(c => c.Quantity),
            CheckoutCartItems = items.Select(item => new CheckoutCartItemVm
            {
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