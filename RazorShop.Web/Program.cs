using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web.Apis;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("RazorShop");
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

builder.Services.AddDbContext<RazorShopDbContext>(options => {
    if (env == "Development")
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Debug);
    }

    options.UseSqlite(connStr!);
});

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

app.UseSession();
app.UseStatusCodePages();
app.UseStaticFiles();
app.SiteApi();
app.ShoppingCartApi();
app.CheckoutCartApi();
app.ProductApi();

app.Logger.LogInformation($"RazorShop App Start - Environment:{env}");

app.Run();