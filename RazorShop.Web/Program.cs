using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Repos;
using RazorShop.Data.Entities;
using RazorShop.Web.Apis;
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

builder.Services.AddAntiforgery();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options => {
    //options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options => {
        options.Cookie.Name = "MyAppCookie";
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(90);
        options.SlidingExpiration = false;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<RazorShopDbContext>();
    db.Database.EnsureCreated();

    var sizes = await db.Sizes!.ToListAsync();
    var sizeTypes = await db.SizeTypes!.ToListAsync();
    var categories = await db.Categories!.ToListAsync();

    var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
    var options = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove);
    cache.Set("sizes", sizes, options);
    cache.Set("sizeTyeps", sizeTypes, options);
    cache.Set("categories", categories, options);
}

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithRedirects("/Redirects?statusCode={0}");

app.Use(async (context, next) => {
    if (!context.Request.Cookies.TryGetValue("CartSessionId", out var cartSessionGuid))
    {
        var guid = Guid.NewGuid();
        cartSessionGuid = guid.ToString();
        context.Response.Cookies.Append("CartSessionId", cartSessionGuid);

        var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RazorShopDbContext>();
        dbContext.Carts!.Add(new Cart { CartGuid = guid, Created = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
    }

    context.Items["CartSessionId"] = cartSessionGuid;

    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseSession();
app.UseStatusCodePages();
app.UseStaticFiles();   
app.SiteApi();
app.CartApi();
app.CheckoutApi();
app.ProductApi();
app.AdminApi();

app.Logger.LogInformation($"RazorShop App Start - Environment:{env}");

app.Run();