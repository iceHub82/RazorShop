namespace RazorShop.Web.ViewModels;

public class CartVm
{
    public int CartItemsCount { get; set; }
    public List<CartItemVm>? CartItems { get; set; } = new();
    public string? SubTotal { get; set; }
    public string? Total { get; set; }
    public string? Tax { get; set; }
}

public class CartItemVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Price { get; set; }
}