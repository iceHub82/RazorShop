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
        app.MapGet("/", async (RazorShopDbContext db, ImagesRepo imgRepo) =>
        {
            ProductsVm vm = new();
            vm.Products = await db.Products!
                .AsNoTracking()
                .Where(p => p.StatusId == 2)
                .Select(p => new ProductVm { Id = p.Id, Name = p.Name, Price = $"{p.Price:#.00} kr" })
                .ToListAsync();

            foreach (var product in vm.Products)
                product.TicksStamp = await imgRepo.GetMainProductImageTickStamp(product.Id);

            return Results.Extensions.RazorSlice<Home, ProductsVm>(vm);
        });

        app.MapGet("/categories", (HttpContext http, IMemoryCache cache) =>
        {
            if (!ApiUtil.IsHtmx(http.Request))
                return Results.BadRequest();

            var categories = ((IEnumerable<Category>)cache.Get("categories")!).Where(c => c.StatusId == 2);

            return Results.Extensions.RazorSlice<Slices.Menu, IEnumerable<Category>>(categories);
        });

        app.MapGet("/About", (HttpContext http, IConfiguration config) =>
        {
            return Results.Extensions.RazorSlice<About>();
        });

        app.MapPost("/newsletter", async (HttpContext http, RazorShopDbContext db) =>
        {
            if (!ApiUtil.IsHtmx(http.Request))
                return Results.BadRequest();

            var form = await http.Request.ReadFormAsync();

            var email = form["newsletter"];

            await db.Contacts!.AddAsync(new Contact { Email = email, Newsletter = true });
            await db.SaveChangesAsync();

            var result = """
                <h5>Tilmeld dig vores nyhedsbrev</h5>
                <div class="d-flex flex-column flex-sm-row w-100 gap-2">
                    <input id="newsletter" type="email" class="form-control" placeholder="Email" maxlength="100" pattern="^[a-zA-Z0-9.!#$%&’*+\/=?^_`\{\|\}~\-]+@@[a-zA-Z0-9\-]+(?:\.[a-zA-Z0-9\-]+)+$" required>
                    <button type="submit" class="btn btn-primary">Tilmeld</button>
                </div>
                <small>Tak for din tilmelding</small>
            """;

            return Results.Content(result);
        });

        app.MapGet("/footer", (HttpContext http, IConfiguration config) =>
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

            return Results.Extensions.RazorSlice<Slices.Footer, FooterVm>(vm);
        });

        app.MapGet("/terms", (IConfiguration config) =>
        {
            var vm = new TermsVm();
            vm.ShopName = config["Shop:Name"];
            vm.Address = config["Shop:Address"];
            vm.City = config["Shop:City"];
            vm.ZipCode = config["Shop:ZipCode"];
            vm.Cvr = config["Shop:Cvr"];
            vm.Email = config["Shop:Email:Contact"];

            return Results.Extensions.RazorSlice<Pages.Terms, TermsVm>(vm);
        });

        app.MapGet("/datapolicy", (IConfiguration config) =>
        {
            var vm = new DataPolicyVm();
            vm.ShopName = config["Shop:Name"];
            vm.Address = config["Shop:Address"];
            vm.City = config["Shop:City"];
            vm.ZipCode = config["Shop:ZipCode"];
            vm.Email = config["Shop:Email:Contact"];

            return Results.Extensions.RazorSlice<DataPolicy, DataPolicyVm>(vm);
        });

        app.MapGet("/customerservice", (IConfiguration config) =>
        {
            var vm = new CustomerServiceVm();
            vm.ShopName = config["Shop:Name"];
            vm.Address = config["Shop:Address"];
            vm.City = config["Shop:City"];
            vm.ZipCode = config["Shop:ZipCode"];
            vm.Email = config["Shop:Email:Contact"];

            return Results.Extensions.RazorSlice<CustomerService, CustomerServiceVm>(vm);
        });

        app.MapGet("/PayAndDelivery", (IConfiguration config) =>
        {
            var vm = new PayAndDeliveryVm();
            vm.ShopName = config["Shop:Name"];

            return Results.Extensions.RazorSlice<PayAndDelivery, PayAndDeliveryVm>(vm);
        });

        app.MapGet("/Redirects", (int statusCode) =>
        {

            if (statusCode == 404)
            {

            }

            return Results.Extensions.RazorSlice<Pages.NotFound>();
        });

        app.MapGet("/Error", (HttpContext context) =>
        {
            if (ApiUtil.IsHtmx(context.Request))
            {
                context.Response.Headers["HX-Target"] = "body";

                return Results.Extensions.RazorSlice<Pages.Error>();
            }

            // Access exception details, if needed
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            // Log the exception or handle it as needed
            Console.WriteLine($"Unhandled exception: {exception?.Message}");

            // Return a custom response or redirect
            //return Results.Problem(
            //    detail: "An unexpected error occurred.",
            //    statusCode: 500
            //);

            return Results.Extensions.RazorSlice<Pages.Error>();
        });
    }
}