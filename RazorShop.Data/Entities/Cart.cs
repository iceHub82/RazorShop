using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class Cart
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public Guid CartGuid { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
    public ICollection<CartItem>? CartItems { get; set; }
}