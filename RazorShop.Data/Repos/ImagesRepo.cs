using Microsoft.EntityFrameworkCore;

namespace RazorShop.Data.Repos;

public class ImagesRepo(RazorShopDbContext db)
{
    private readonly RazorShopDbContext _db = db;

    public async Task<string> GetPrimaryProductImageTickStamp(int id)
    {
        var img = await _db.ProductImages!
        .Where(x => x.ProductId == id && x.Image!.Primary)
        .Select(x => x.Image!.Updated ?? x.Image.Created)
        .FirstOrDefaultAsync();

        return img.Ticks.ToString() ?? string.Empty;
    }
}