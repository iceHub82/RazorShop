﻿namespace RazorShop.Web.Models.ViewModels;

public class AdminVm
{

}

public class AdminProductsVm
{
    public List<AdminProductVm> AdminProducts { get; set; } = new();
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
}

public class AdminProductVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Price { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? TicksStamp { get; set; }
    public string? AdminProductFormAntiForgeryToken { get; set; }
    public string? AdminProductFormMainImageAntiForgeryToken { get; set; }
    //public List<AdminProductSizeVm>? AdminProductSizes { get; set; } = new();
    public List<AdminImageVm>? AdminImageVms { get; set; } = new();
}

public class AdminImageVm
{
    public int Id { get; set; }
    public string? TicksStamp { get; set; }
}

public class AdminProductSizeVm
{
    public int Id { get; set; }
}