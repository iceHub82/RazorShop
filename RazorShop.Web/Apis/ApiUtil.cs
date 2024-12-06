namespace RazorShop.Web.Apis;

public static class ApiUtil
{
    public static bool IsHtmx(HttpRequest request)
    {
        return request.Headers["hx-request"] == "true";
    }

    public static RouteHandlerBuilder NoCache(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (http, next) => {
            var result = await next(http);
            http.HttpContext.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            http.HttpContext.Response.Headers.Pragma = "no-cache";
            http.HttpContext.Response.Headers.Expires = "0";
            return result;
        });
    }
}