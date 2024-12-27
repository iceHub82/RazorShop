using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using RazorShop.Web.Slices.Admin;

namespace RazorShop.Web.Apis;

public static class AdminApis
{
    public static void AdminApi(this WebApplication app)
    {
        app.MapGet("/Admin", (HttpContext http) => {

            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Home>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Home>();

        }).RequireAuthorization();

        app.MapGet("/Admin/Orders", (HttpContext http) => {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Orders>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Orders>();

        }).RequireAuthorization();

        app.MapGet("/Admin/Products", (HttpContext http) => {
            if (ApiUtil.IsHtmx(http.Request))
            {
                //http.Response.Headers.Append("Vary", "HX-Request");
                return Results.Extensions.RazorSlice<Products>();
            }

            return Results.Extensions.RazorSlice<Pages.Admin.Products>();

        }).RequireAuthorization();

        app.MapGet("/Login", () => { 
            return Results.Extensions.RazorSlice<Pages.Admin.Login>();
        });

        app.MapPost("/Login", async (HttpContext context) =>
        {
            var username = context.Request.Form["username"];
            var password = context.Request.Form["password"];

            // Dummy authentication logic
            if (username == "admin" && password == "password")
            {
                var claims = new List<Claim> { 
                    new(ClaimTypes.Name, username!), 
                    new(ClaimTypes.Role, "Admin") 
                };

                var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await context.SignInAsync("MyCookieAuth", claimsPrincipal);

                return Results.Extensions.RazorSlice<Home>();
            }

            return Results.Redirect("/Login");
        });
    }
}