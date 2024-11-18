namespace RazorShop.Web.Apis;

public static class ApiUtil
{
    public static bool IsHtmx(HttpRequest request)
    {
        return request.Headers["hx-request"] == "true";
    }
}