namespace RazorShop.Web.ViewModels;

public class ProductVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Price { get; set; }
    public int CheckedSizeId { get; set; }
    public List<ProductSizeVm>? ProductSizes { get; set; } = new();
}

public class ProductSizeVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
}