using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class Size
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? Name { get; set; }
    public int SizeTypeId { get; set; }
    public SizeType? SizeType { get; set; }

    public ICollection<ProductSize> ProductSizes { get; set; }
}