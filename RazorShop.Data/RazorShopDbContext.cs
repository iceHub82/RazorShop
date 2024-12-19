using Microsoft.EntityFrameworkCore;
using RazorShop.Data.Entities;
using System.Reflection.Emit;

namespace RazorShop.Data;

public class RazorShopDbContext : DbContext
{
    public RazorShopDbContext(DbContextOptions<RazorShopDbContext> options) : base(options)
    { }

    public DbSet<Product>? Products { get; set; }
    public DbSet<ProductImage>? ProductImages { get; set; }
    public DbSet<Image>? Images { get; set; }
    public DbSet<Category>? Categories { get; set; }
    public DbSet<Cart>? Carts { get; set; }
    public DbSet<CartItem>? CartItems { get; set; }
    public DbSet<Size>? Sizes { get; set; }
    public DbSet<SizeType>? SizeTypes { get; set; }
    public DbSet<ProductSize>? ProductSizes { get; set; }
    public DbSet<Order>? Orders { get; set; }
    public DbSet<Address>? Addresses { get; set; }
    public DbSet<Country>? Countries { get; set; }
    public DbSet<Contact>? Contacts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ProductSize>()
        .HasKey(ps => new { ps.ProductId, ps.SizeId });

        builder.Entity<ProductImage>()
        .HasKey(pi => new { pi.ProductId, pi.ImageId });

        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);

        //modelBuilder.Entity<Category>()
        //    .HasMany(c => c.SubCategories)
        //    .WithOne(c => c.ParentCategory)
        //    .HasForeignKey(c => c.ParentCategoryId);

        SeedData.Seed(builder);
    }
}