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
        app.MapGet("/checkout", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache) =>
        {
            //var cart = await GetCart(http, db);
            //var items = await GetCartItems(cart.Id, db)!;
            //var vm = GetShoppingCartViewModel(items!, cache);

            return Results.Extensions.RazorSlice<Slices.Checkout>();
        });
    }
}