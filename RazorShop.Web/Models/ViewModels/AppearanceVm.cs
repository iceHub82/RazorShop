namespace RazorShop.Web.Models.ViewModels;

public class AppearanceVm
{
    public string Current { get; set; } = "editorial";
    public string? AntiForgeryToken { get; set; }
    public bool Saved { get; set; }
}
