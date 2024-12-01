using System.ComponentModel.DataAnnotations;

namespace RazorShop.Web.Models.ViewModels;

public class CartVm
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Price { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
}

public class ShoppingCartItemVm : CartVm
{

}

public class CheckoutCartItemVm : CartVm
{

}

public class ShoppingCartVm
{
    public List<ShoppingCartItemVm>? ShoppingCartItems { get; set; } = new();
    public string? ShoppingCartTotal { get; set; }
    public int ShoppingCartQuantity { get; set; }
}

public class CheckoutCartVm
{
    public List<CheckoutCartItemVm>? CheckoutCartItems { get; set; } = new();
    public string? CheckoutCartTotal { get; set; }
    public int CheckoutCartQuantity { get; set; }
}

public class UpdateCheckoutCartVm
{
    public ShoppingCartVm? ShoppingCartVm { get; set; }
    public CheckoutCartVm? CheckoutCartVm { get; set; }
}