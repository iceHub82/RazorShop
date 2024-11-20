namespace RazorShop.Web.Models.ViewModels;

public class CartVm
{
    public List<CartItemVm>? CartItems { get; set; } = new();
    public string? Total { get; set; }
    public int CartQuantity { get; set; }
}

public class CartItemVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Price { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
}