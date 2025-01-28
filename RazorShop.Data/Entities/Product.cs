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
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public int StatusId { get; set; }
    public Status? Status { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<ProductSize>? ProductSizes { get; set; }
    public ICollection<ProductImage>? ProductImages { get; set; }
}