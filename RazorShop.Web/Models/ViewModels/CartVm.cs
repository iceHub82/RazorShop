namespace RazorShop.Web.Models.ViewModels;

public class ItemVm
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Price { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
    public string? TicksStamp { get; set; }
}

public class CartItemVm : ItemVm
{

}

public class CheckoutItemVm : ItemVm
{

}

public class CartVm
{
    public List<CartItemVm>? CartItems { get; set; } = new();
    public string? CartTotal { get; set; }
    public int CartQuantity { get; set; }
    public string? Delivery { get; set; }
}

public class CheckoutVm
{
    public List<CheckoutItemVm>? CheckoutItems { get; set; } = new();
    public string? CheckoutTotal { get; set; }
    public int CheckoutQuantity { get; set; }
    public string? CheckoutFormAntiForgeryToken { get; set; }
    public string? Delivery { get; set; }
    public string? VAT { get; set; }
    public string? ShopName { get; set; }
}