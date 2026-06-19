using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Data.Repos;
using RazorShop.Web.Models.ViewModels;

using Size = RazorShop.Data.Entities.Size;

namespace RazorShop.Web.Apis;

public static class CartApis
{
    public static void CartApi(this WebApplication app)
    {
        app.MapGet("/cart", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, ImagesRepo imgRepo, IAntiforgery antiforgery) =>
        {
            if (ApiUtil.IsHtmx(http.Request))
            {
                var cart = await ApiUtil.GetCart(http, db);
                var items = await ApiUtil.GetCartItems(cart.Id, db)!;
                var vm = await GetCartViewModel(items!, cache, imgRepo);
                vm!.AntiForgeryToken = antiforgery.GetAndStoreTokens(http).RequestToken;

                return Results.RazorSlice<Slices.Cart, CartVm>(vm!);
            }

            return Results.RazorSlice<Pages.CheckoutEmpty>();

        }).NoCache();

        app.MapPost("/cart/add/{id}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, ImagesRepo imgRepo, IAntiforgery antiforgery, ILogger<object> log, int id) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(http);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest();
            }

            IFormCollection form;
            try
            {
                form = await http.Request.ReadFormAsync();
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Cart add: failed to read form body");
                return Results.BadRequest("Could not read form");
            }

            if (!int.TryParse(form["size"], out var size)) size = 0;
            if (!int.TryParse(form["quantity"], out var quantity) || quantity <= 0 || quantity > 100)
                return Results.BadRequest();

            var cart = await ApiUtil.GetCart(http, db);

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

            var items = await ApiUtil.GetCartItems(cart.Id, db)!;
            var vm = await GetCartViewModel(items, cache, imgRepo);
            vm!.AntiForgeryToken = antiforgery.GetAndStoreTokens(http).RequestToken;

            return Results.RazorSlice<Slices.Cart, CartVm>(vm!);
        });

        app.MapDelete("/cart/delete/{id}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, ImagesRepo imgRepo, IAntiforgery antiforgery, int id) =>
        {
            try
            {
                await antiforgery.ValidateRequestAsync(http);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest("Antiforgery token invalid");
            }

            var cart = await ApiUtil.GetCart(http, db);

            var item = await db.CartItems!.FirstOrDefaultAsync(c => c.Id == id && c.CartId == cart.Id);
            if (item == null)
                return Results.NotFound();

            item.Deleted = true;
            item.Updated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var items = await ApiUtil.GetCartItems(cart.Id, db)!;
            var vm = await GetCartViewModel(items, cache, imgRepo);
            vm!.AntiForgeryToken = antiforgery.GetAndStoreTokens(http).RequestToken;

            return Results.RazorSlice<Slices.CartDelete, CartVm>(vm!);
        });
    }

    private static async Task<CartVm?> GetCartViewModel(List<CartItem> items, IMemoryCache cache, ImagesRepo imgRepo)
    {
        if (items.Count == 0)
            return new CartVm();

        var sizes = (IEnumerable<Size>)cache.Get("sizes")!;

        var vm = new CartVm {
            CartQuantity = items.Sum(c => c.Quantity),
            CartItems = items.Select(item => new CartItemVm {
                ProductId = item.ProductId,
                Id = item.Id,
                Name = item.Product!.Name,
                Description = item.Product.Description,
                Price = $"{item.Product.Price:#.00} kr",
                Size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name,
                Quantity = item.Quantity,
            }).ToList(),
            Delivery = "49.00 kr",
            CartTotal = $"{items.Sum(c => c.Product!.Price * c.Quantity) + 49:#.00} kr"
        };

        foreach (var item in vm.CartItems)
            item.TicksStamp = await imgRepo.GetMainProductImageTickStamp(item.ProductId);

        return vm;
    }
}