namespace RazorShop.Web.Models.ViewModels;

public class ShoppingCartVm
{
    public int ShoppingCartQuantity { get; set; }
    public List<ShoppingCartItemVm>? ShoppingCartItems { get; set; } = new();
    public string? ShoppingCartTotal { get; set; }
}

public class ShoppingCartItemVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Price { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
}