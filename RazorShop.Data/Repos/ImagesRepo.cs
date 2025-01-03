using Microsoft.EntityFrameworkCore;

namespace RazorShop.Data.Repos;

public class ImagesRepo(RazorShopDbContext db)
{
    private readonly RazorShopDbContext _db = db;

    public async Task<string> GetMainProductImageTickStamp(int id)
    {
        var img = await _db.ProductImages!
        .Where(x => x.ProductId == id && x.Image!.Main)
        .Select(x => x.Image!.Updated ?? x.Image.Created)
        .FirstOrDefaultAsync();

        return img.Ticks.ToString() ?? string.Empty;
    }

    public async Task<string> GetGalleryProductImageTickStamp(int id)
    {
        var img = await _db.ProductImages!
        .Where(x => x.ImageId == id)
        .Select(x => x.Image!.Updated ?? x.Image.Created)
        .FirstOrDefaultAsync();

        return img.Ticks.ToString() ?? string.Empty;
    }
}