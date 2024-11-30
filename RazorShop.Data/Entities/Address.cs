using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RazorShop.Data.Entities;

public class Address
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; } 
    public string? StreetName { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public int CountryId { get; set; }
    public Country? Country { get; set; }
}