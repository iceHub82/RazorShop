namespace RazorShop.Web.Models.ViewModels;

public class ShopCartVm
{
    public int ShopCartItemsCount { get; set; }
    public List<ShopCartItemVm>? ShopCartItems { get; set; } = new();
    public string? Total { get; set; }
}

public class ShopCartItemVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Price { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
}