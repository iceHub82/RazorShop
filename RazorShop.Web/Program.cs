using Microsoft.EntityFrameworkCore;
using RazorShop.Data;
using RazorShop.Data.Entities;
using RazorShop.Web;

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

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    scope.ServiceProvider.GetRequiredService<RazorShopDbContext>().Database.EnsureCreated();
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
        dbContext.Carts.Add(new Cart { CartGuid = guid, Created = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();
    }

    context.Items["CartSessionId"] = cartSessionGuid;

    await next();
});

//app.UseRouting();

app.UseSession();
//app.UseStatusCodePages();
app.UseStaticFiles();
app.MinimalApi();

app.Logger.LogInformation($"RazorShop App Start - Environment:{env}");

app.Run();