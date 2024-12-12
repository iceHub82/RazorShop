using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Models.ViewModels;
using Quickpay.Services;
using Quickpay.RequestParams;
using Quickpay.Models.Payments;

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
                return Results.Extensions.RazorSlice<Pages.CheckoutEmpty>();

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
                return Results.Extensions.RazorSlice<Slices.CheckoutEmpty>();

            var vm = GetCheckoutViewModel(items, cache);

            return Results.Extensions.RazorSlice<Slices.CheckoutUpdate, CheckoutVm>(vm!);
        });

        app.MapGet("/checkout/address-bill", (string? addressbillCb) =>
        {
            if (addressbillCb == "on")
                return Results.Extensions.RazorSlice<Slices.AddressBilling>();

            return Results.Content(string.Empty);
        });

        app.MapPost("/checkout/submit", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, IConfiguration config) =>
        {
            using var transaction = db.Database.BeginTransaction();

            try
            {
                await antiforgery.ValidateRequestAsync(http); // Validate token

                var form = await http.Request.ReadFormAsync();

                var contact = new Contact();
                contact.Email = form["email"]; ;
                contact.PhoneNumber = form["phone"];
                contact.Newsletter = form["newsletter"] == "on";

                await db.Contacts!.AddAsync(contact);
                await db.SaveChangesAsync();

                var address = new Address();
                address.FirstName = form["first-name"];
                address.LastName = form["last-name"];
                address.StreetName = form["address"];
                address.ZipCode = form["zip-code"];
                address.City = form["city"];
                address.CountryId = int.Parse(form["country-id"]!);

                await db.Addresses!.AddAsync(address);
                await db.SaveChangesAsync();

                AddressBill? addressBill = null;
                if (form["addressbillCb"] == "on")
                {
                    addressBill = new();
                    addressBill.FirstName = form["bill-first-name"];
                    addressBill.LastName = form["bill-last-name"];
                    addressBill.StreetName = form["bill-address"];
                    addressBill.ZipCode = form["bill-zip-code"];
                    addressBill.City = form["bill-city"];

                    await db.AddressBills!.AddAsync(addressBill);
                    await db.SaveChangesAsync();
                }

                var cart = await GetCart(http, db) ?? throw new Exception("Cart not found");

                var order = new Order();
                order.CartId = cart.Id;
                order.Created = DateTime.UtcNow;
                order.AddressId = address.Id;
                order.AddressBillId = addressBill == null ? null : addressBill!.Id;
                order.ContactId = contact.Id;
                order.StatusId = 1;

                await db.Orders!.AddAsync(order);
                await db.SaveChangesAsync();

                transaction.Commit();

                var qpApiKey = config["PaymentApiKey"];
                var ps = new PaymentsService(qpApiKey);

                // First we must create a payment and for this we need a CreatePaymentRequestParams object
                var createPaymentParams = new CreatePaymentRequestParams("DKK", createRandomOrderId());
                createPaymentParams.text_on_statement = "QuickPay .NET Example";

                var basketItemJeans = new Basket();
                basketItemJeans.qty = 1;
                basketItemJeans.item_name = "Jeans";
                basketItemJeans.item_price = 100;
                basketItemJeans.vat_rate = 0.25;
                basketItemJeans.item_no = "123";

                var basketItemShirt = new Basket();
                basketItemShirt.qty = 1;
                basketItemShirt.item_name = "Shirt";
                basketItemShirt.item_price = 300;
                basketItemShirt.vat_rate = 0.25;
                basketItemShirt.item_no = "321";

                createPaymentParams.basket = new Basket[] { basketItemJeans, basketItemShirt };

                var payment = await ps.CreatePayment(createPaymentParams);

                // Second we must create a payment link for the payment. This payment link can be opened in a browser to show the payment window from QuickPay.
                var createPaymentLinkParams = new CreatePaymentLinkRequestParams((int)((basketItemJeans.qty * basketItemJeans.item_price + basketItemShirt.qty * basketItemShirt.item_price) * 100));
                createPaymentLinkParams.payment_methods = "creditcard";
                //createPaymentLinkParams.callback_url = "/Callback";
                createPaymentLinkParams.auto_capture = true; // This will automatically capture the payment right after it has been authorized.

                var paymentLink = await ps.CreateOrUpdatePaymentLink(payment.id, createPaymentLinkParams);

                var test = $"""
                    <script>
                        window.location.href = '{paymentLink.url}';
                    </script>
                """;
                return Results.Content(test);
            }
            catch (AntiforgeryValidationException ex)
            {
                Console.WriteLine("AntiforgeryValidationException was thrown");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Processing is cancelled.");
            }

            return Results.Content(string.Empty);
        });
    }

    private static string createRandomOrderId()
    {
        return Guid.NewGuid().ToString().Replace("-", "")[..20];
    }

    private static async Task<Cart> GetCart(HttpContext http, RazorShopDbContext db)
    {
        if (http.Request.Cookies.TryGetValue("CartSessionId", out var cartSessionGuid))
        {
            var existingCart = await db.Carts!.FirstOrDefaultAsync(c => c.CartGuid == Guid.Parse(cartSessionGuid!));

            if (existingCart != null)
                return existingCart;
        }

        var guid = Guid.NewGuid();
        cartSessionGuid = guid.ToString();
        http.Response.Cookies.Append("CartSessionId", cartSessionGuid);

        var newCart = new Cart { CartGuid = guid, Created = DateTime.UtcNow };
        db.Carts!.Add(newCart);
        await db.SaveChangesAsync();

        return newCart;
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
                Price = $"{item.Product.Price:00.#} kr.",
                Size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name,
                Quantity = item.Quantity
            }).ToList(),
            Delivery = "49 kr.",
            CheckoutTotal = $"{(items.Sum(c => c.Product!.Price * c.Quantity)) + 49:00.#} kr."
        };
    }
}