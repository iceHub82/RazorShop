using Microsoft.EntityFrameworkCore;
using RazorShop.Data.Entities;

namespace RazorShop.Data;

public class RazorShopDbContext : DbContext
{
    public RazorShopDbContext(DbContextOptions<RazorShopDbContext> options) : base(options)
    { }

    public DbSet<Product>? Products { get; set; }
    public DbSet<Category>? Categories { get; set; }
    public DbSet<Cart>? Carts { get; set; }
    public DbSet<CartItem>? CartItems { get; set; }
    public DbSet<Size>? Sizes { get; set; }
    public DbSet<SizeType>? SizeTypes { get; set; }
    public DbSet<ProductSize>? ProductSizes { get; set; }
    public DbSet<Order>? Orders { get; set; }
    public DbSet<Address>? Addresses { get; set; }
    public DbSet<AddressBill>? AddressBills { get; set; }
    public DbSet<Country>? Countries { get; set; }
    public DbSet<Contact>? Contacts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ProductSize>()
        .HasKey(ps => new { ps.ProductId, ps.SizeId });

        builder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);

        //modelBuilder.Entity<Category>()
        //    .HasMany(c => c.SubCategories)
        //    .WithOne(c => c.ParentCategory)
        //    .HasForeignKey(c => c.ParentCategoryId);

        builder.Entity<SizeType>().HasData(
            new SizeType { Id = 1, Name = "Paper" },
            new SizeType { Id = 2, Name = "Clothing" }
        );

        builder.Entity<Size>().HasData(
            new Size { Id = 1, Name = "A4", SizeTypeId = 1 },
            new Size { Id = 2, Name = "A3", SizeTypeId = 1 },
            new Size { Id = 3, Name = "Small", SizeTypeId = 2 },
            new Size { Id = 4, Name = "Medium", SizeTypeId = 2 },
            new Size { Id = 5, Name = "Large", SizeTypeId = 2 },
            new Size { Id = 6, Name = "X-Large", SizeTypeId = 2 }
        );

        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Drawings" },
            new Category { Id = 2, Name = "Clothing" }
        );

        builder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Drawing 1", Description = "Generative plot drawing 1", Price = 499.00m, CategoryId = 1 },
            new Product { Id = 2, Name = "Drawing 2", Description = "Generative plot drawing 2", Price = 249.00m, CategoryId = 1 },
            new Product { Id = 3, Name = "Drawing 3", Description = "Generative plot drawing 3", Price = 349.00m, CategoryId = 1 },
            new Product { Id = 4, Name = "T-Shirt 1", Description = "T-Shirt with motive 1", Price = 149.00m, CategoryId = 2 },
            new Product { Id = 5, Name = "T-Shirt 2", Description = "T-Shirt with motive 2", Price = 299.00m, CategoryId = 2 },
            new Product { Id = 6, Name = "T-Shirt 3", Description = "T-Shirt with motive 3", Price = 99.00m, CategoryId = 2 }
        );

        builder.Entity<ProductSize>().HasData(
            new ProductSize { ProductId = 1, SizeId = 1 },
            new ProductSize { ProductId = 1, SizeId = 2 },
            new ProductSize { ProductId = 2, SizeId = 1 },
            new ProductSize { ProductId = 2, SizeId = 2 },
            new ProductSize { ProductId = 3, SizeId = 2 },

            new ProductSize { ProductId = 4, SizeId = 3 },
            new ProductSize { ProductId = 4, SizeId = 4 },
            new ProductSize { ProductId = 4, SizeId = 5 },
            new ProductSize { ProductId = 4, SizeId = 6 },
            new ProductSize { ProductId = 5, SizeId = 5 }
        );

        builder.Entity<Country>().HasData(
            new Country { Id = 1, Name = "Danmark" },
            new Country { Id = 2, Name = "Færøerne" },
            new Country { Id = 3, Name = "Grønland" },
            new Country { Id = 4, Name = "Island" }
        );
    }
}