using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RazorShop.Data.Entities;
using RazorShop.Web.Apis;
using Xunit;

namespace RazorShop.Tests;

// JSON-LD is emitted raw into <script> tags, so it must always be well-formed JSON
// and carry the schema.org types Google expects.
public class JsonLdTests
{
    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Shop:Link"] = "https://shop.test",
            ["Shop:Name"] = "TestShop",
        }).Build();

    private static HttpContext Http()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("shop.test");
        return ctx;
    }

    [Fact]
    public void Product_jsonld_is_well_formed_with_product_and_breadcrumb()
    {
        var product = new Product
        {
            Id = 7,
            Name = "Widget",
            Description = "A widget",
            Price = 499m,
            StatusId = (int)EntityStatus.Active,
            Category = new Category { Name = "Gadgets" },
        };

        var json = ProductApis.BuildProductJsonLd(product, Http(), Config());

        using var doc = JsonDocument.Parse(json);   // throws on malformed JSON
        var root = doc.RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());
        Assert.Equal("Product", root[0].GetProperty("@type").GetString());
        Assert.Equal("Widget", root[0].GetProperty("name").GetString());
        // price formatted invariant, two decimals, in-stock because StatusId == Active
        Assert.Equal("499.00", root[0].GetProperty("offers").GetProperty("price").GetString());
        Assert.Equal("https://schema.org/InStock", root[0].GetProperty("offers").GetProperty("availability").GetString());
        Assert.Equal("BreadcrumbList", root[1].GetProperty("@type").GetString());
    }

    [Fact]
    public void Site_jsonld_is_well_formed_with_organization_and_website()
    {
        var json = SiteApis.BuildSiteJsonLd(Http(), Config());

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(2, root.GetArrayLength());
        Assert.Equal("Organization", root[0].GetProperty("@type").GetString());
        Assert.Equal("WebSite", root[1].GetProperty("@type").GetString());
    }
}
