using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Navigation property for Sizes
    public ICollection<ProductSize>? ProductSizes { get; set; }
}