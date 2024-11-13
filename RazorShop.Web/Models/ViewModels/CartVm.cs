namespace RazorShop.Web.Models.ViewModels;

public class CartVm
{
    public int CartItemsCount { get; set; }
    public List<CartItemVm>? CartItems { get; set; } = new();
    public string? Total { get; set; }
}

public class CartItemVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Price { get; set; }
    public string? Size { get; set; }
}