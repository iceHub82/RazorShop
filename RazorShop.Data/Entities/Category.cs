using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public int StatusId { get; set; }
    public Status? Status { get; set; }
    public ICollection<Product>? Products { get; set; }

    // Optional self-referencing property for nested categories
    //public int? ParentCategoryId { get; set; }
    //public Category? ParentCategory { get; set; }
    //public ICollection<Category>? SubCategories { get; set; }
}