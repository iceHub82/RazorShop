using Microsoft.Extensions.DependencyInjection;
using Quickpay.RequestParams;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Apis;
using Xunit;

namespace RazorShop.Tests;

// End-to-end /Success: boots the real app, fakes only the QuickPay gateway, and checks
// the order's paid state actually flips (or doesn't) based on what the gateway reports.
public class SuccessFlowTests
{
    [Fact]
    public async Task Settled_payment_marks_order_paid()
    {
        const string reference = "settled-ref-001";
        const int paymentId = 5001;

        // Gateway reports an accepted, captured payment bound to THIS order's reference.
        var gateway = new FakePaymentGateway(
            new GatewayPayment(paymentId, Accepted: true, OrderId: reference, State: "captured"));

        using var factory = new ShopAppFactory { PaymentGatewayOverride = gateway };
        SeedOrder(factory, reference, paymentId);

        var response = await factory.CreateClient().GetAsync($"/Success/{reference}");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal((int)EntityStatus.Active, StatusOf(factory, reference));  // flipped to paid
    }

    [Fact]
    public async Task Unverified_payment_leaves_order_unpaid()
    {
        const string reference = "pending-ref-002";
        const int paymentId = 5002;

        // Gateway reports a payment that is not yet settled.
        var gateway = new FakePaymentGateway(
            new GatewayPayment(paymentId, Accepted: false, OrderId: reference, State: "pending"));

        using var factory = new ShopAppFactory { PaymentGatewayOverride = gateway };
        SeedOrder(factory, reference, paymentId);

        var response = await factory.CreateClient().GetAsync($"/Success/{reference}");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal((int)EntityStatus.New, StatusOf(factory, reference));  // still unpaid
    }

    private static void SeedOrder(ShopAppFactory factory, string reference, int paymentId)
    {
        using var scope = factory.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<RazorShopDbContext>();

        var cart = new Cart { CartGuid = Guid.NewGuid(), Created = DateTime.UtcNow };
        var address = new Address { FirstName = "Test", LastName = "Buyer", City = "Copenhagen" };
        var contact = new Contact { Email = "buyer@example.com" };
        db.Carts!.Add(cart);
        db.Addresses!.Add(address);
        db.Contacts!.Add(contact);
        db.SaveChanges();

        db.Orders!.Add(new Order
        {
            Reference = reference,
            CartId = cart.Id,
            AddressId = address.Id,
            ContactId = contact.Id,
            StatusId = (int)EntityStatus.New,
            QuickPayPaymentId = paymentId,
            Created = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private static int StatusOf(ShopAppFactory factory, string reference)
    {
        using var scope = factory.NewScope();
        var db = scope.ServiceProvider.GetRequiredService<RazorShopDbContext>();
        return db.Orders!.First(o => o.Reference == reference).StatusId;
    }
}

internal sealed class FakePaymentGateway(GatewayPayment? payment) : IPaymentGateway
{
    public Task<GatewayPayment?> GetPayment(int paymentId) => Task.FromResult(payment);

    public Task<GatewayPayment> CreatePayment(CreatePaymentRequestParams request) =>
        throw new NotSupportedException("not exercised by /Success tests");

    public Task<string> CreatePaymentLinkUrl(int paymentId, CreatePaymentLinkRequestParams request) =>
        throw new NotSupportedException("not exercised by /Success tests");
}
