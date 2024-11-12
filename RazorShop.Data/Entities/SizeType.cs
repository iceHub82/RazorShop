using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class SizeType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? Name { get; set; }

    // Navigation property for Sizes
    public ICollection<Size>? Sizes { get; set; }
}