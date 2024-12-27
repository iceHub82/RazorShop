using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace RazorShop.Web.Apis;

public static class AdminApis
{
    public static void AdminApi(this WebApplication app)
    {
        app.MapGet("/Admin", () => {
            return Results.Extensions.RazorSlice<Pages.Admin>();
        }).RequireAuthorization();

        app.MapGet("/Login", () => { 
            return Results.Extensions.RazorSlice<Pages.Login>();
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

                return Results.Extensions.RazorSlice<Pages.Admin>();
            }

            return Results.Redirect("/Login");
        });
    }
}