using Microsoft.EntityFrameworkCore;
using RazorShop.Data.Entities;

namespace RazorShop.Data;

internal static class SeedData
{
    public static void Seed(ModelBuilder builder)
    {
        SeedSizeTypes(builder);
        SeedSizes(builder);
        SeedCategories(builder);
        SeedProducts(builder);
        SeedProductSizes(builder);
        SeedImages(builder);
        SeedProductImages(builder);
        SeedCountries(builder);
    }

    private static void SeedSizeTypes(ModelBuilder builder)
    {
        builder.Entity<SizeType>().HasData(
            new SizeType { Id = 1, Name = "Paper" },
            new SizeType { Id = 2, Name = "Clothing" }
        );
    }

    private static void SeedSizes(ModelBuilder builder)
    {
        builder.Entity<Size>().HasData(
            new Size { Id = 1, Name = "A4", SizeTypeId = 1 },
            new Size { Id = 2, Name = "A3", SizeTypeId = 1 },
            new Size { Id = 3, Name = "Small", SizeTypeId = 2 },
            new Size { Id = 4, Name = "Medium", SizeTypeId = 2 },
            new Size { Id = 5, Name = "Large", SizeTypeId = 2 },
            new Size { Id = 6, Name = "X-Large", SizeTypeId = 2 }
        );
    }

    private static void SeedCategories(ModelBuilder builder)
    {
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Drawings" },
            new Category { Id = 2, Name = "Clothing" }
        );
    }

    private static void SeedProducts(ModelBuilder builder)
    {
        builder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Drawing 1", Description = "Generative plot drawing 1", Price = 499.00m, CategoryId = 1 },
            new Product { Id = 2, Name = "Drawing 2", Description = "Generative plot drawing 2", Price = 249.00m, CategoryId = 1 },
            new Product { Id = 3, Name = "Drawing 3", Description = "Generative plot drawing 3", Price = 349.00m, CategoryId = 1 },
            new Product { Id = 4, Name = "T-Shirt 1", Description = "T-Shirt with motive 1", Price = 149.00m, CategoryId = 2 },
            new Product { Id = 5, Name = "T-Shirt 2", Description = "T-Shirt with motive 2", Price = 299.00m, CategoryId = 2 },
            new Product { Id = 6, Name = "T-Shirt 3", Description = "T-Shirt with motive 3", Price = 99.00m, CategoryId = 2 }
        );
    }

    private static void SeedProductSizes(ModelBuilder builder)
    {
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
    }

    private static void SeedImages(ModelBuilder builder)
    {
        builder.Entity<Image>().HasData(
            new Image { Id = 1, FileName = "", ContentType = "webp", Primary = true }
            //new Image { ProductId = 1, ImageId = 2 },
            //new Image { ProductId = 1, ImageId = 3 },
            //new Image { ProductId = 2, ImageId = 2 },
            //new Image { ProductId = 3, ImageId = 2 },
            //new Image { ProductId = 4, ImageId = 3 }
        );
    }

    private static void SeedProductImages(ModelBuilder builder)
    {
        builder.Entity<ProductImage>().HasData(
            new ProductImage { ProductId = 1, ImageId = 1 }
            //new ProductImage { ProductId = 1, ImageId = 2 },
            //new ProductImage { ProductId = 1, ImageId = 3 },
            //new ProductImage { ProductId = 2, ImageId = 2 },
            //new ProductImage { ProductId = 3, ImageId = 2 },
            //new ProductImage { ProductId = 4, ImageId = 3 }
        );
    }

    private static void SeedCountries(ModelBuilder builder)
    {
        builder.Entity<Country>().HasData(
            new Country { Id = 1, Name = "Danmark" },
            new Country { Id = 2, Name = "Færøerne" },
            new Country { Id = 3, Name = "Grønland" },
            new Country { Id = 4, Name = "Island" }
        );
    }
}