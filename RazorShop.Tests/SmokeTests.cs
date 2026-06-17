using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RazorShop.Web.Apis;
using Xunit;

namespace RazorShop.Tests;

// Boots the whole app (full DI graph + real routing) against a throwaway SQLite file.
// This is the test that fails fast on a missing service registration or broken middleware
// pipeline — the class of bug a unit test can't see.
public class SmokeTests : IClassFixture<ShopAppFactory>
{
    private readonly ShopAppFactory _factory;

    public SmokeTests(ShopAppFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/Product/1")]
    [InlineData("/sitemap.xml")]
    [InlineData("/terms")]
    [InlineData("/datapolicy")]
    public async Task Get_endpoints_return_success(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.True(response.IsSuccessStatusCode, $"{url} returned {(int)response.StatusCode}");
    }

    [Fact]
    public async Task Home_is_wired_to_the_editorial_theme()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        Assert.Contains("data-theme=\"editorial\"", html);
        Assert.Contains("theme-editorial.css", html);
    }
}

public class ShopAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"razorshop-test-{Guid.NewGuid():N}.db");

    // When set, replaces the real QuickPay gateway so tests never hit the network.
    public IPaymentGateway? PaymentGatewayOverride { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RazorShopDb"] = $"DataSource={_dbPath}",
                ["EmailProvider:Host"] = "",  // forces EmailHandler.SendEmail to no-op (no real SMTP)
            }));

        builder.ConfigureServices(services =>
        {
            if (PaymentGatewayOverride is not null)
            {
                services.RemoveAll<IPaymentGateway>();
                services.AddSingleton(PaymentGatewayOverride);
            }
        });
    }

    public IServiceScope NewScope() =>
        Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
