namespace RazorShop.Web.Models.ViewModels;

public class ProductsVm
{
    public string? Category { get; set; }
    public List<ProductVm>? Products { get; set; } = new();
}

public class ProductVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Price { get; set; }
    public string? TicksStamp { get; set; }
    public int CheckedSizeId { get; set; }
    public List<ProductSizeVm>? ProductSizes { get; set; } = new();
}

public class ProductSizeVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
}