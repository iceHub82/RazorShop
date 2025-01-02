using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class Image
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? FileName { get; set; }
    public bool Primary { get; set; }
    public string? ContentType { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }

    public ICollection<ProductImage>? ProductImages { get; set; }
}