using RazorShop.Web.Apis;
using Xunit;

namespace RazorShop.Tests;

public class ReferenceTests
{
    [Fact]
    public void Reference_is_20_hex_chars_with_no_dashes()
    {
        var r = CheckoutApis.GenerateReference();

        Assert.Equal(20, r.Length);
        Assert.Matches("^[0-9a-f]{20}$", r);
    }

    [Fact]
    public void References_do_not_collide()
    {
        var seen = new HashSet<string>();
        for (var i = 0; i < 1000; i++)
            Assert.True(seen.Add(CheckoutApis.GenerateReference()), "duplicate order reference generated");
    }
}
