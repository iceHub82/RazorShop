using RazorShop.Web.Apis;
using Xunit;

namespace RazorShop.Tests;

// IsPaymentSettled is the money decision: it gates flipping an order to Paid and
// sending the confirmation email. Every "false" path here is an order we must NOT
// treat as paid.
public class PaymentVerificationTests
{
    private const string Ref = "abc123";

    [Theory]
    [InlineData("processed")]
    [InlineData("captured")]
    [InlineData("CAPTURED")]   // state match is case-insensitive
    public void Accepted_matching_reference_and_settled_state_is_paid(string state)
    {
        Assert.True(CheckoutApis.IsPaymentSettled(accepted: true, orderId: Ref, state: state, reference: Ref));
    }

    [Fact]
    public void Not_accepted_is_unpaid()
    {
        Assert.False(CheckoutApis.IsPaymentSettled(accepted: false, orderId: Ref, state: "processed", reference: Ref));
    }

    [Theory]
    [InlineData("new")]
    [InlineData("pending")]
    [InlineData("rejected")]
    [InlineData(null)]
    public void Non_settled_state_is_unpaid(string? state)
    {
        Assert.False(CheckoutApis.IsPaymentSettled(accepted: true, orderId: Ref, state: state, reference: Ref));
    }

    [Fact]
    public void Reference_mismatch_is_unpaid()
    {
        // A real, accepted, captured payment — but for a DIFFERENT order. Must not pass.
        Assert.False(CheckoutApis.IsPaymentSettled(accepted: true, orderId: "someone-elses-order", state: "captured", reference: Ref));
    }

    [Fact]
    public void Reference_match_is_case_sensitive()
    {
        Assert.False(CheckoutApis.IsPaymentSettled(accepted: true, orderId: "ABC123", state: "captured", reference: Ref));
    }

    [Fact]
    public void Missing_payment_data_is_unpaid()
    {
        // Mirrors a null payment from the gateway: accepted defaults false, fields null.
        Assert.False(CheckoutApis.IsPaymentSettled(accepted: false, orderId: null, state: null, reference: Ref));
    }
}
