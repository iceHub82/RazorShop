namespace RazorShop.Web.Models;

public class LayoutModel
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? JsonLd { get; set; }
    public string? Image { get; set; } // og:image path (relative to Shop:Link); null = site default
    public bool HasHeader { get; set; }
}