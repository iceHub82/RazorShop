using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Email;
using RazorShop.Web.Models.ViewModels;
using Quickpay.Services;
using Quickpay.RequestParams;
using Quickpay.Models.Payments;

using Address = RazorShop.Data.Entities.Address;
using RazorShop.Data.Repos;

namespace RazorShop.Web.Apis;

public static class CheckoutApis
{
    public static void CheckoutApi(this WebApplication app)
    {
        app.MapGet("/checkout", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, IAntiforgery antiforgery, ImagesRepo imgRepo) =>
        {
            var cart = await GetCart(http, db);

            var items = await GetCartItems(cart.Id, db)!;
            if (items.Count == 0)
                return Results.Extensions.RazorSlice<Pages.CheckoutEmpty>();

            var vm = await GetCheckoutViewModel(items, cache, imgRepo);

            var tokens = antiforgery.GetAndStoreTokens(http);
            vm!.CheckoutFormAntiForgeryToken = tokens.RequestToken;

            return Results.Extensions.RazorSlice<Pages.Checkout, CheckoutVm>(vm!);
        });

        app.MapGet("/checkout/update/{itemId}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, int itemId, int quantity, ImagesRepo imgRepo) =>
        {
            var result = await UpdateCartItemQuantity(db, itemId, quantity);

            var cart = await GetCart(http, db);
            var items = await GetCartItems(cart.Id, db)!;

            var vm = await GetCheckoutViewModel(items, cache, imgRepo);

            return Results.Extensions.RazorSlice<Slices.CheckoutUpdate, CheckoutVm>(vm!);
        });

        app.MapDelete("/checkout/delete/{id}", async (HttpContext http, RazorShopDbContext db, IMemoryCache cache, ImagesRepo imgRepo, int id) =>
        {
            var cart = await GetCart(http, db);

            var item = db.CartItems!.Find(id);
            item!.Deleted = true;
            item!.Updated = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var items = await GetCartItems(cart.Id, db)!;
            if (items.Count == 0)
                return Results.Extensions.RazorSlice<Slices.CheckoutEmpty>();

            var vm = await GetCheckoutViewModel(items, cache, imgRepo);

            return Results.Extensions.RazorSlice<Slices.CheckoutUpdate, CheckoutVm>(vm!);
        });

        app.MapGet("/checkout/address-bill", (string? addressbillCb) =>
        {
            if (addressbillCb == "on")
                return Results.Extensions.RazorSlice<Slices.AddressBilling>();

            return Results.Content(string.Empty);
        });

        app.MapPost("/checkout/submit", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery, IConfiguration config, ILogger<object> log) =>
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

                Address? addressBill = null;
                if (form["addressbillCb"] == "on")
{
                    addressBill = new();
                    addressBill.FirstName = form["bill-first-name"];
                    addressBill.LastName = form["bill-last-name"];
                    addressBill.StreetName = form["bill-address"];
                    addressBill.ZipCode = form["bill-zip-code"];
                    addressBill.City = form["bill-city"];

                    await db.Addresses!.AddAsync(addressBill);
                    await db.SaveChangesAsync();
                }

                var cart = await GetCart(http, db) ?? throw new Exception("Cart not found");

                var reference = GenerateReference();

                var order = new Order();
                order.Reference = reference;
                order.CartId = cart.Id;
                order.Created = DateTime.UtcNow;
                order.AddressId = address.Id;
                order.AddressBillId = addressBill == null ? null : addressBill!.Id;
                order.ContactId = contact.Id;
                order.StatusId = 1;

                await db.Orders!.AddAsync(order);
                await db.SaveChangesAsync();

                transaction.Commit();

                var paymentKey = config["PaymentApiKey"];

                if (string.IsNullOrEmpty(paymentKey))
                    throw new Exception("No PaymentKey has been provided");

                var items = await GetCartItems(cart.Id, db)!;

                var ps = new PaymentsService(paymentKey);

                // First we must create a payment and for this we need a CreatePaymentRequestParams object
                var createPaymentParams = new CreatePaymentRequestParams("DKK", reference);
                createPaymentParams.text_on_statement = "QuickPay .NET Example";

                var basket = new Basket[items.Count + 1];
                basket[0] = new Basket { item_no = "0", item_name = "Delivery", item_price = 49, qty = 1 };
                for (int i = 0; i < items.Count; i++)
                {
                    basket[i + 1] = new Basket { item_no = items[i].Id.ToString(), item_name = items[i].Product!.Name, item_price = (double)items[i].Product!.Price, qty = items[i].Quantity, vat_rate = 0.25 };
                }

                createPaymentParams.basket = basket;
                var payment = await ps.CreatePayment(createPaymentParams);

                // Second we must create a payment link for the payment. This payment link can be opened in a browser to show the payment window from QuickPay.
                var paymentLinkAmount = (int)(items.Sum(c => c.Product!.Price * c.Quantity)) + 49;

                var createPaymentLinkParams = new CreatePaymentLinkRequestParams(paymentLinkAmount * 100);
                createPaymentLinkParams.payment_methods = "creditcard";

                var domain = $"{http.Request.Scheme}://{http.Request.Host}";
                createPaymentLinkParams.continue_url = $"{domain}/Success/{reference}";
                createPaymentLinkParams.cancel_url = $"{domain}/Unsuccessful/{reference}";
                createPaymentLinkParams.auto_capture = true; //This will automatically capture the payment right after it has been authorized.

                var paymentLink = await ps.CreateOrUpdatePaymentLink(payment.id, createPaymentLinkParams);

                var redirectToPaymentScreenScript = $"""
                    <script>
                        window.location.href = '{paymentLink.url}';
                    </script>
                """;

                return Results.Content(redirectToPaymentScreenScript);
            }
            catch (AntiforgeryValidationException ex)
            {
                log.LogError("AntiforgeryValidationException was thrown");
            }
            catch (Exception ex)
            {
                log.LogError("Processing is cancelled.");
            }

            return Results.Content(string.Empty);
        });

        app.MapGet("/Success/{reference}", async (IWebHostEnvironment env, HttpContext http, IMemoryCache cache, RazorShopDbContext db, string? reference, IConfiguration config, ILogger<object> log) =>
        {
            var vm = new OrderSuccessVm();

            try
            {
                var order = await db.Orders!.Include(o => o.Cart)!.Include(o => o.Address).ThenInclude(o => o!.Country!).Include(o => o.AddressBill).FirstOrDefaultAsync(o => o.Reference == reference);

                if (order == null || order!.StatusId == 2) {

                    log.LogWarning($"Order success page called with unknown reference Id: {reference}");

                    return Results.Extensions.RazorSlice<Pages.OrderSuccess, OrderSuccessVm>(vm); // Change this to error or not found
                }

                order.StatusId = 2; // Paid
                order.Updated = DateTime.UtcNow;
                await db.SaveChangesAsync();

                var address = order.AddressBill ?? order.Address;

                var addressHtml = GenerateAddressHtml(address!);

                var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";

                var items = await GetCartItems(order.Cart!.Id, db)!;

                var sizes = (IEnumerable<Data.Entities.Size>)cache.Get("sizes")!;

                var productsHtml = GenerateProductsHtmlStringInDanish(baseUrl, items, sizes, "DKK", 49);

                var handler = new EmailHandler(config);

                var dateStr = DateTime.Now.ToString("dd. MMMM yyyy", new CultureInfo("da-DK"));

                //var emailLogoPath = $"{baseUrl}/img/email-logo.png";
                var emailTemplatePath = $"{env.WebRootPath}/templates/email/order-success-email-danish.htm";

                var content = handler.CreateMessageBody(emailTemplatePath, /*customer.Name,*/ addressHtml, dateStr, order.Reference!, productsHtml/*, emailLogoPath*/);

                var contact = await db.Contacts!.FirstAsync(c => c.Id == order.ContactId);

                var message = new Message([contact.Email!], $"Din bestilling er modtaget: {order.Reference}", content);
                handler.SendEmail(message);

                http.Session.Clear();
                http.Response.Cookies.Delete("CartSessionId");

                //if (order != null)
                //{

                //}
                //else
                //{
                //    log.LogWarning($"Order success page called with unknown reference Id: {referenceId}");
                //}

                return Results.Extensions.RazorSlice<Pages.OrderSuccess, OrderSuccessVm>(vm);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Order success page error with reference Id: {reference}");

                return Results.Extensions.RazorSlice<Pages.OrderSuccess, OrderSuccessVm>(vm);
            }
        });

        app.MapGet("/Unsuccessful/{referenceId}", async (RazorShopDbContext db, string? referenceId, ILogger<object> log) =>
        {
            if (!await db.Orders!.AnyAsync(o => o.Reference == referenceId))
            {
                log.LogWarning($"Unsuccessful order page called with unknown reference Id: {referenceId}");
            }

            return Results.Content(string.Empty);
        });
    }

    private static string GenerateReference()
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

    private static async Task<CheckoutVm?> GetCheckoutViewModel(List<CartItem> items, IMemoryCache cache, ImagesRepo imgRepo)
    {
        if (items.Count == 0)
            return new CheckoutVm();

        var sizes = (IEnumerable<Data.Entities.Size>)cache.Get("sizes")!;

        var total = items.Sum(c => c.Product!.Price * c.Quantity) + 49;
        var vat = total * 0.25m;

        var vm = new CheckoutVm {
            CheckoutQuantity = items.Sum(c => c.Quantity),
            CheckoutItems = items.Select(item => new CheckoutItemVm {
                Id = item.Id,
                ProductId = item.ProductId,
                Name = item.Product!.Name,
                Description = item.Product.Description,
                Price = $"{item.Product.Price * item.Quantity:#.00} kr",
                Size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name,
                Quantity = item.Quantity
            }).ToList(),
            VAT = $"{vat:#.00} kr",
            Delivery = "49.00 kr",
            CheckoutTotal = $"{total:#.00} kr"
        };

        foreach (var item in vm.CheckoutItems)
            item.TicksStamp = await imgRepo.GetMainProductImageTickStamp(item.ProductId);

        return vm;
    }

    private static string GenerateAddressHtml(Address address)
    {
        var addressStr = string.Empty;

        addressStr += $"{address.FirstName} {address.LastName}<br>";
        addressStr += $"{address.StreetName}<br>";
        addressStr += $"{address.ZipCode} {address.City}<br>";

        if (address.Country != null)
            addressStr += $"{address.Country!.Name}<br>";

        return addressStr;
    }

    private static string GenerateProductsHtmlStringInDanish(string baseUrl, List<CartItem> items, IEnumerable<Data.Entities.Size> sizes, string countryCode, /*Currency currency,*/ decimal delivery)
    {
        
        var mailStr = string.Empty;
        //var discount = false;

        var totalPrice = 0.0m;

        var i = 0;
        foreach (var item in items)
        {
            var productId = item.Product!.Id;

            var mainImgPath = $"{baseUrl}/products/{productId}/{productId}_thumbnail.webp";

            var price = item.Product!.Price;
            var quantity = item.Quantity;
            var quantityPrice = price * quantity;

            var size = sizes.FirstOrDefault(s => s.Id == item.SizeId)?.Name;

            mailStr += "<table role='presentation' border='0' cellpadding='0' cellspacing='0' width='100%'>";
            mailStr += "<tbody>";
            mailStr += "<tr>";
            mailStr += "<td width='77' align='left' valign='top' style='padding-right:12px'>";
            mailStr += $"<a href='{baseUrl}/product/{productId}' target='_blank'>";
            mailStr += $"<img src='{mainImgPath}' alt='&nbsp;' width='77' style='display: block; background-color: rgb(55, 55, 55) !important; line-height: 111px; font-size: 1px;'>";
            mailStr += "</a>";
            mailStr += "</td>";
            mailStr += $"<td align='left' valign='top'><table border='0' cellpadding='0' cellspacing='0' width='100%' role='presentation'><tbody><tr><td><table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%'><tbody><tr><td style='font-family:Tiempos,Times New Roman,serif; color:#1A1A1A; font-size:14px; line-height:20px; letter-spacing:0px'>{item.Product.Name}</td></tr><tr><td style='font-family:HelveticaNow,Helvetica,sans-serif; color:#1A1A1A; font-size:14px; line-height:20px; letter-spacing:0px'>Antal: {item.Quantity}</td></tr><tr><td style='font-family:HelveticaNow,Helvetica,sans-serif; font-size:14px; line-height:20px; letter-spacing:0px; color:#66676E'>Størrelse: {size} </td></tr></tbody></table></td><td align='right' valign='top' style='font-family:HelveticaNow,Helvetica,sans-serif; color:#1A1A1A; font-size:14px; line-height:20px; letter-spacing:0px'>{price:#.00} kr </td></tr></tbody></table><table border='0' cellpadding='0' cellspacing='0' width='100%' role='presentation'><tbody><tr><td aria-hidden='true' height='8' style='font-size:0px; line-height:0px'>&nbsp;</td></tr><tr><td align='left'></td></tr></tbody></table></td>";
            mailStr += "";
            mailStr += "</tr>";
            mailStr += "</tbody>";
            mailStr += "</table>";
            mailStr += "<table border='0' cellpadding='0' cellspacing='0' width='100%' aria-hidden='true'><tbody><tr><td height='12' style='font-size:0px; line-height:0px'>&nbsp;</td></tr></tbody></table>";

            if (items.Count - 1 == i)
            {
                mailStr += "<table border='0' cellpadding='0' cellspacing='0' width='100%' aria-hidden='true'><tbody><tr><td height='12' style='font-size:0px; line-height:0px'>&nbsp;</td></tr></tbody></table>";
                mailStr += "<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%'><tbody><tr><td><table border='0' cellpadding='0' cellspacing='0' width='100%' bgcolor='rgb(71, 72, 74)' aria-hidden='true' data-ogsb='' data-ogab='#D0D1D3' style='background-color: rgb(71, 72, 74) !important;'><tbody><tr><td height='1' style='font-size:0px; line-height:0px'>&nbsp;</td></tr></tbody></table><table border='0' cellpadding='0' cellspacing='0' width='100%' aria-hidden='true'><tbody><tr><td height='12' style='font-size:0px; line-height:0px'>&nbsp;</td></tr></tbody></table></td></tr></tbody></table>";
            }
            i++;

            totalPrice += quantityPrice;
        }

        var successMailStyleBottom = "width='50%' style='font-family:HelveticaNow,Helvetica,sans-serif; font-size:14px; line-height:20px; letter-spacing:0px; color:#1A1A1A;'";

        mailStr += "<table role='presentation' cellpadding='0' cellspacing='0' border='0' width='100%'>";
        mailStr += "<tbody>";
        mailStr += "<tr>";
        mailStr += $"<td {successMailStyleBottom}>Betalingsform</td>";
        mailStr += $"<td align='right' {successMailStyleBottom}>Betalingskort</td>";
        mailStr += "</tr>";
        mailStr += "<tr>";
        mailStr += $"<td {successMailStyleBottom}>Købsbeløb</td>";
        mailStr += $"<td align='right' {successMailStyleBottom}>{totalPrice:#.00} kr</td>";
        //successMailStr += $"<td align='right' {successMailStyleBottom}>{totalPrice} {currency.Symbol}</td>";
        mailStr += "</tr>";

        //if (discount)
        //{
        //    successMailStr += "<tr>";
        //    successMailStr += $"<td {successMailStyleBottom}>Rabat</td>";
        //    successMailStr += $"<td align='right' {successMailStyleBottom}>{"Discount"} {currency.Symbol}</td>";
        //    successMailStr += "</tr>";
        //}

        var deliveryStr = delivery == 0.0m ? "Gratis" : $"{delivery:#.00} kr" /*+ $" {currency.Symbol}"*/;

        mailStr += "<tr>";
        mailStr += $"<td {successMailStyleBottom}>Levering</td>";
        mailStr += $"<td align='right' {successMailStyleBottom}>{deliveryStr}</td>";
        mailStr += "</tr>";

        mailStr += "<tr><td aria-hidden='true' colspan='2' height='8' style='font-size:0px; line-height:0px'>&nbsp;</td></tr>";

        mailStr += "</tr>";
        mailStr += "<tr>";
        mailStr += "<td width='50%' align='left' valign='top' style='font-family:HelveticaNow,Helvetica,sans-serif;'><span style='font-size:18px; line-height:24px; letter-spacing:-0.18px; color:#1A1A1A; font-weight:bold'>Total</span>&nbsp;<span style='font-family: HelveticaNow, Helvetica, sans-serif, serif, EmojiFont; font-size: 12px; letter-spacing: 0px; line-height: 16px; color: rgb(102, 103, 110); text-align: left;'>inkl. moms</span></td>";
        //successMailStr += $"<td width='50%' align='right' valign='top' style='font-family:HelveticaNow,Helvetica,sans-serif; font-size:18px; line-height:24px; letter-spacing:-0.18px; color:#1A1A1A; font-weight:bold'>{totalPrice + delivery} {currency.Symbol}</td>";
        mailStr += $"<td width='50%' align='right' valign='top' style='font-family:HelveticaNow,Helvetica,sans-serif; font-size:18px; line-height:24px; letter-spacing:-0.18px; color:#1A1A1A; font-weight:bold'>{totalPrice + delivery:#.00} kr</td>";
        mailStr += "</tr>";
        mailStr += "</tbody>";
        mailStr += "</table>";

        return mailStr;
    }
}