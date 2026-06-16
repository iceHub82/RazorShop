using Quickpay.RequestParams;
using Quickpay.Services;

namespace RazorShop.Web.Apis;

// Just the QuickPay payment data the app actually reads, so tests can fake the
// gateway without constructing vendor SDK types.
public sealed record GatewayPayment(int Id, bool Accepted, string? OrderId, string? State);

public interface IPaymentGateway
{
    Task<GatewayPayment> CreatePayment(CreatePaymentRequestParams request);
    Task<string> CreatePaymentLinkUrl(int paymentId, CreatePaymentLinkRequestParams request);
    Task<GatewayPayment?> GetPayment(int paymentId);
}

// Thin adapter over the QuickPay SDK. Callers still guard on PaymentApiKey before use,
// so the key is read lazily here rather than in the constructor.
public sealed class QuickPayGateway(IConfiguration config) : IPaymentGateway
{
    private PaymentsService Service() => new(config["PaymentApiKey"]!);

    public async Task<GatewayPayment> CreatePayment(CreatePaymentRequestParams request)
    {
        var p = await Service().CreatePayment(request);
        return new GatewayPayment(p.id, p.accepted, p.order_id, p.state);
    }

    public async Task<string> CreatePaymentLinkUrl(int paymentId, CreatePaymentLinkRequestParams request)
    {
        var link = await Service().CreateOrUpdatePaymentLink(paymentId, request);
        return link.url;
    }

    public async Task<GatewayPayment?> GetPayment(int paymentId)
    {
        var p = await Service().GetPayment(paymentId, null);
        return p is null ? null : new GatewayPayment(p.id, p.accepted, p.order_id, p.state);
    }
}
