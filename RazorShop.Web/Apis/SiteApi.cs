using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Xml;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Web.Models.ViewModels;
using RazorShop.Data.Repos;
using RazorShop.Data.Entities;
using RazorShop.Web.Pages;

namespace RazorShop.Web.Apis;

public static class SiteApis
{
    public static void SiteApi(this WebApplication app)
    {
        app.MapGet("/", async (RazorShopDbContext db, HttpContext http, ImagesRepo imgRepo, IConfiguration config) =>
        {
            ProductsVm vm = new();
            vm.Products = await db.Products!
                .AsNoTracking()
                .Where(p => p.StatusId == (int)EntityStatus.Active)
                .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
                .ToListAsync();

            foreach (var product in vm.Products)
                product.TicksStamp = await imgRepo.GetMainProductImageTickStamp(product.Id);

            vm.JsonLd = BuildSiteJsonLd(http, config);

            return Results.RazorSlice<Home, ProductsVm>(vm);
        });

        app.MapGet("/categories", (HttpContext http, IMemoryCache cache) =>
        {
            if (!ApiUtil.IsHtmx(http.Request))
                return Results.BadRequest();

            var categories = ((IEnumerable<Category>)cache.Get("categories")!).Where(c => c.StatusId == (int)EntityStatus.Active);

            return Results.RazorSlice<Slices.Menu, IEnumerable<Category>>(categories);
        });

        app.MapGet("/About", (HttpContext http, IConfiguration config) =>
        {
            return Results.RazorSlice<About>();
        });

        app.MapPost("/newsletter", async (HttpContext http, RazorShopDbContext db, IAntiforgery antiforgery) =>
        {
            if (!ApiUtil.IsHtmx(http.Request))
                return Results.BadRequest();

            try
            {
                await antiforgery.ValidateRequestAsync(http);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest();
            }

            var form = await http.Request.ReadFormAsync();

            var email = form["newsletter"].ToString();
            if (string.IsNullOrWhiteSpace(email) || email.Length > 100 || !new EmailAddressAttribute().IsValid(email))
                return Results.BadRequest();

            // Skip duplicate signups; still return the success UI so we don't leak who is subscribed.
            var alreadySubscribed = await db.Contacts!.AnyAsync(c => c.Email == email && c.Newsletter);
            if (!alreadySubscribed)
            {
                await db.Contacts!.AddAsync(new Contact { Email = email, Newsletter = true });
                await db.SaveChangesAsync();
            }

            var result = """
                <h5>Tilmeld dig vores nyhedsbrev</h5>
                <div class="d-flex flex-column flex-sm-row w-100 gap-2">
                    <input id="newsletter" type="email" class="form-control" placeholder="Email" maxlength="100" pattern="^[a-zA-Z0-9.!#$%&’*+\/=?^_`\{\|\}~\-]+@@[a-zA-Z0-9\-]+(?:\.[a-zA-Z0-9\-]+)+$" required>
                    <button type="submit" class="btn btn-primary">Tilmeld</button>
                </div>
                <small>Tak for din tilmelding</small>
            """;

            return Results.Content(result);
        }).RequireRateLimiting("newsletter");

        app.MapGet("/footer", (HttpContext http, IConfiguration config, IAntiforgery antiforgery) =>
        {
            if (!ApiUtil.IsHtmx(http.Request))
                return Results.BadRequest();

            var vm = new FooterVm();
            vm.ShopName = config["Shop:Name"];
            vm.Year = DateTime.Now.Year.ToString();
            vm.Address = config["Shop:Address"];
            vm.City = config["Shop:City"];
            vm.ZipCode = config["Shop:ZipCode"];
            vm.Email = config["Shop:Email:Contact"];

            var token = antiforgery.GetAndStoreTokens(http);
            vm.AntiForgeryToken = token.RequestToken;

            return Results.RazorSlice<Slices.Footer, FooterVm>(vm);
        });

        app.MapGet("/terms", (IConfiguration config) =>
        {
            var vm = new SiteVm();
            vm.ShopName = config["Shop:Name"];
            vm.Address = config["Shop:Address"];
            vm.City = config["Shop:City"];
            vm.ZipCode = config["Shop:ZipCode"];
            vm.Cvr = config["Shop:Cvr"];
            vm.Email = config["Shop:Email:Contact"];

            return Results.RazorSlice<Pages.Terms, SiteVm>(vm);
        });

        app.MapGet("/datapolicy", (IConfiguration config) =>
        {
            var vm = new SiteVm();
            vm.ShopName = config["Shop:Name"];
            vm.Address = config["Shop:Address"];
            vm.City = config["Shop:City"];
            vm.ZipCode = config["Shop:ZipCode"];
            vm.Email = config["Shop:Email:Contact"];

            return Results.RazorSlice<DataPolicy, SiteVm>(vm);
        });

        app.MapGet("/customerservice", (IConfiguration config) =>
        {
            var vm = new SiteVm();
            vm.ShopName = config["Shop:Name"];
            vm.Address = config["Shop:Address"];
            vm.City = config["Shop:City"];
            vm.ZipCode = config["Shop:ZipCode"];
            vm.Email = config["Shop:Email:Contact"];

            return Results.RazorSlice<CustomerService, SiteVm>(vm);
        });

        app.MapGet("/PayAndDelivery", (IConfiguration config) =>
        {
            var vm = new SiteVm();
            vm.ShopName = config["Shop:Name"];

            return Results.RazorSlice<PayAndDelivery, SiteVm>(vm);
        });

        app.MapGet("/Redirects", (int statusCode) =>
        {
            return Results.RazorSlice<Pages.NotFound>();
        });

        app.MapGet("/sitemap.xml", async (RazorShopDbContext db, HttpContext http, IConfiguration config) =>
        {
            var baseUrl = (config["Shop:Link"]?.TrimEnd('/'))
                ?? $"{http.Request.Scheme}://{http.Request.Host}";

            var products = await db.Products!
                .AsNoTracking()
                .Where(p => p.StatusId == (int)EntityStatus.Active)
                .Select(p => new { p.Id, p.Updated, p.Created })
                .ToListAsync();

            var sb = new StringBuilder();
            using (var xml = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true }))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                void WriteUrl(string loc, DateTime? lastmod, string changefreq, string priority)
                {
                    xml.WriteStartElement("url");
                    xml.WriteElementString("loc", loc);
                    if (lastmod.HasValue)
                        xml.WriteElementString("lastmod", lastmod.Value.ToString("yyyy-MM-dd"));
                    xml.WriteElementString("changefreq", changefreq);
                    xml.WriteElementString("priority", priority);
                    xml.WriteEndElement();
                }

                WriteUrl($"{baseUrl}/", null, "daily", "1.0");
                WriteUrl($"{baseUrl}/Products", null, "daily", "0.9");
                WriteUrl($"{baseUrl}/About", null, "monthly", "0.4");
                WriteUrl($"{baseUrl}/Terms", null, "yearly", "0.3");
                WriteUrl($"{baseUrl}/DataPolicy", null, "yearly", "0.3");
                WriteUrl($"{baseUrl}/CustomerService", null, "monthly", "0.4");
                WriteUrl($"{baseUrl}/PayAndDelivery", null, "monthly", "0.4");

                foreach (var p in products)
                    WriteUrl($"{baseUrl}/Product/{p.Id}", p.Updated ?? p.Created, "weekly", "0.8");

                xml.WriteEndElement();
                xml.WriteEndDocument();
            }

            return Results.Content(sb.ToString(), "application/xml", Encoding.UTF8);
        });

        app.MapGet("/Error", (HttpContext context) =>
        {
            if (ApiUtil.IsHtmx(context.Request))
            {
                context.Response.Headers["HX-Target"] = "body";

                return Results.RazorSlice<Pages.Error>();
            }

            // Access exception details, if needed
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            // Log the exception or handle it as needed
            Console.WriteLine($"Unhandled exception: {exception?.Message}");

            return Results.RazorSlice<Pages.Error>();
        });
    }

    internal static string BuildSiteJsonLd(HttpContext http, IConfiguration config)
    {
        var baseUrl = (config["Shop:Link"]?.TrimEnd('/'))
            ?? $"{http.Request.Scheme}://{http.Request.Host}";
        var shopName = config["Shop:Name"] ?? "Artform.dk";

        var organization = new Dictionary<string, object?> {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = shopName,
            ["url"] = baseUrl,
            ["logo"] = $"{baseUrl}/img/logo/logo2.svg",
            ["sameAs"] = new[] {
                "https://www.instagram.com/artform.dk",
                "https://www.facebook.com/artform.dk",
            },
        };

        var website = new Dictionary<string, object?> {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = shopName,
            ["url"] = baseUrl,
            ["inLanguage"] = "da-DK",
        };

        return JsonSerializer.Serialize(new object[] { organization, website });
    }
}