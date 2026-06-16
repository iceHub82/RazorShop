namespace RazorShop.Data.Entities;

// Mirrors the seeded Status table rows (see SeedData.SeedStatuses).
// StatusId columns are FKs to those rows; orders reuse Active to mean "paid".
public enum EntityStatus
{
    New = 1,
    Active = 2,
    Pending = 3,
    Cancelled = 4,
}
