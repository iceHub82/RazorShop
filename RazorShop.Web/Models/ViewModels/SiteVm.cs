namespace RazorShop.Web.Models.ViewModels;

public class SiteVm
{
    public string? ShopName { get; set; }
    public string? Year { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public string? Cvr { get; set; }
    public string? Email { get; set; }
}

public class FooterVm : SiteVm
{
}

public class TermsVm : SiteVm
{
}

public class DataPolicyVm : SiteVm
{
}

public class CustomerServiceVm : SiteVm
{
}

public class PayAndDeliveryVm : SiteVm
{
}