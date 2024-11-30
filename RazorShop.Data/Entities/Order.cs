using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public int? BillingAddressId { get; set; }
    public BillingAddress? BillingAddress { get; set; }
    public int ContactId { get; set; }
    public Contact? Contact { get; set; }
    public int CartId { get; set; }
    public Cart? Cart { get; set; }
    public int StatusId { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}