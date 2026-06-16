using Microsoft.EntityFrameworkCore;
using RazorShop.Data;
using RazorShop.Data.Entities;

namespace RazorShop.Web.Apis;

public static class ApiUtil
{
    public static async Task<Cart> GetCart(HttpContext http, RazorShopDbContext db)
    {
        if (http.Request.Cookies.TryGetValue("CartSessionId", out var cartSessionGuid))
        {
            var existingCart = await db.Carts!.FirstOrDefaultAsync(c => c.CartGuid == Guid.Parse(cartSessionGuid!));

            if (existingCart != null)
                return existingCart;
        }

        var guid = Guid.NewGuid();
        cartSessionGuid = guid.ToString();
        http.Response.Cookies.Append("CartSessionId", cartSessionGuid, CartCookieOptions(http.Request));

        var newCart = new Cart { CartGuid = guid, Created = DateTime.UtcNow };
        db.Carts!.Add(newCart);
        await db.SaveChangesAsync();

        return newCart;
    }

    public static async Task<List<CartItem>> GetCartItems(int cartId, RazorShopDbContext db)
    {
        return await db.CartItems!.Where(c => c.CartId == cartId && !c.Deleted).Include(c => c.Product).ToListAsync();
    }

    public static bool IsHtmx(HttpRequest request)
    {
        return request.Headers["hx-request"] == "true";
    }

    public static CookieOptions CartCookieOptions(HttpRequest request) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddDays(30),
    };

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

    public static DataTablesParameters GetDatatableParameters(HttpRequest request, HashSet<string> allowedSortColumns, string defaultSort)
    {
        var search = request.Query["search[value]"].FirstOrDefault();
        var draw = request.Query["draw"].FirstOrDefault();
        _ = int.TryParse(request.Query["start"].FirstOrDefault(), out var skip);
        if (!int.TryParse(request.Query["length"].FirstOrDefault(), out var take) || take <= 0 || take > 200)
            take = 10;

        _ = int.TryParse(request.Query["order[0][column]"].FirstOrDefault(), out var orderIndex);
        var dirRaw = request.Query["order[0][dir]"].FirstOrDefault();
        var dir = string.Equals(dirRaw, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var sortRaw = request.Query[$"columns[{orderIndex}][name]"].FirstOrDefault();
        var sort = !string.IsNullOrEmpty(sortRaw) && allowedSortColumns.Contains(sortRaw) ? sortRaw : defaultSort;

        return new DataTablesParameters {
            Search = search!,
            Draw = draw!,
            Skip = skip < 0 ? 0 : skip,
            Take = take,
            OrderIndex = orderIndex,
            Sort = sort,
            SortDirection = dir
        };
    }
}

public class DataTablesParameters
{
    public string? Search { get; set; }
    public string? Draw { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int OrderIndex { get; set; }
    public string? Sort { get; set; }
    public string? SortDirection { get; set; }
}