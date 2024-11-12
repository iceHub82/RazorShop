var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromSeconds(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


var connectionString = builder.Configuration.GetConnectionString("FastShopConnection");
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

builder.Services.AddDbContext<FastShopDbContext>(options => {
    if (env == "Development")
    {
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, LogLevel.Debug);
    }

    options.UseSqlite(connectionString!);
});


var app = builder.Build();



app.MapGet("/", () => "Hello World!");

app.Run();
