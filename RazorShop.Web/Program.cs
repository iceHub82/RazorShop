using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Repos;
using RazorShop.Data.Entities;
using RazorShop.Web.Apis;
using RazorShop.Web.Apis.Admin;
using RazorShop.Web.Apis.Settings;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) => {
    config.ReadFrom.Configuration(context.Configuration);
});

var connStr = builder.Configuration.GetConnectionString("RazorShopDb");
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

builder.Services.AddDbContext<RazorShopDbContext>(options => {
    if (env == "Local")
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Debug);
    }

    options.UseSqlite(connStr!);
});

builder.Services.AddTransient<ImagesRepo>();

builder.Services.Configure<FormOptions>(options => {
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB total per multipart request
    options.ValueLengthLimit = 1 * 1024 * 1024;
});

builder.Services.AddAntiforgery();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options => {
    //options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication("App_Auth")
    .AddCookie("App_Auth", options => {
        options.Cookie.Name = "App_Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(90);
        options.SlidingExpiration = false;
    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminOnly", p => p.RequireAuthenticatedUser().RequireRole("Admin"));
});

builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.AddPolicy("newsletter", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<RazorShopDbContext>();
    db.Database.EnsureCreated();

    var statuses = await db.Statuses!.ToListAsync();
    var sizes = await db.Sizes!.ToListAsync();
    var sizeTypes = await db.SizeTypes!.ToListAsync();
    var categories = await db.Categories!.ToListAsync();

    var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
    var options = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
    cache.Set("statuses", statuses, options);
    cache.Set("sizes", sizes, options);
    cache.Set("sizeTypes", sizeTypes, options);
    cache.Set("categories", categories, options);
}

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) => {
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    headers["Cross-Origin-Resource-Policy"] = "same-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), accelerometer=(), gyroscope=(), magnetometer=()";
    // style-src still permits 'unsafe-inline' because Bootstrap utilities and inline style="..."
    // attributes are used across views. Move to CSP nonces in a follow-up to drop it.
    // cdn.datatables.net is admin-only (DataTables JS+CSS); cdn.jsdelivr.net serves Bootstrap Icons.
    // When served to localhost (dev), allow loopback connect-src so Visual Studio Browser Link works.
    var isLocalhost = context.Request.Host.Host is "localhost" or "127.0.0.1" or "::1";
    var connectSrc = isLocalhost
        ? "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*; "
        : "connect-src 'self'; ";
    headers["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data:; " +
        connectSrc +
        "script-src 'self' https://cdn.datatables.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.datatables.net https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net data:; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self' https://payment.quickpay.net";
    await next();
});

app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithRedirects("/Redirects?statusCode={0}");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.UseSession();
app.UseStatusCodePages();

app.UseStaticFiles(new StaticFileOptions {
    OnPrepareResponse = context => {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".css", ".js", ".webp", ".svg" };
        var fileExtension = Path.GetExtension(context.File.Name);
        if (extensions.Contains(fileExtension))
            context.Context.Response.Headers.CacheControl = "public, max-age=31536000";
    }
});

app.SiteApi();
app.CartApi();
app.CheckoutApi();
app.ProductApi();
app.AdminApi();
app.SettingsApi();

app.Logger.LogInformation($"RazorShop App Start - Environment:{env}");

app.Run();