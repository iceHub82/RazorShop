namespace RazorShop.Web.Models.ViewModels;

public class AdminVm
{

}

public class AdminProductsVm
{
    public List<AdminProductVm> AdminProducts { get; set; } = new();
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
}

public class AdminCategoriesVm
{
    public List<AdminCategoryVm> AdminCategories { get; set; } = new();
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
}

public class AdminSizesVm
{
    public List<AdminSizeVm> AdminSizes { get; set; } = new();
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
}

public class AdminOrdersVm
{
    public List<AdminOrderVm> AdminOrders { get; set; } = new();
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
}

public class AdminOrderVm
{
    public int Id { get; set; }
    public string? Reference { get; set; }
    public string? Created { get; set; }
}

public class AdminProductVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Price { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? TicksStamp { get; set; }
    public int? CategoryId { get; set; }
    public string? AdminProductFormAntiForgeryToken { get; set; }
    public string? AdminProductFormMainImageAntiForgeryToken { get; set; }
    public List<AdminSizeVm>? AdminSizes { get; set; } = new();
    public List<AdminImageVm>? AdminImageVms { get; set; } = new();
    public List<AdminCategoryVm>? AdminCategories { get; set; } = new();
}

public class AdminNewProductVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Price { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? AdminNewProductFormAntiForgeryToken { get; set; }
    public List<AdminCategoryVm>? AdminCategories { get; set; } = new();
    public List<AdminSizeVm>? AdminSizes { get; set; } = new();
}

public class AdminImageVm
{
    public int Id { get; set; }
    public string? TicksStamp { get; set; }
}

public class AdminSizeVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Selected { get; set; }
    public string? AdminSizeFormAntiForgeryToken { get; internal set; }
}

public class AdminCategoryVm
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? AdminCategoryFormAntiForgeryToken { get; internal set; }
}