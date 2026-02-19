using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {


        builder.HasKey(x => x.Id);
        builder.Property(x => x.NameAz).IsRequired().HasMaxLength(500);
        builder.Property(x => x.NameRu).IsRequired().HasMaxLength(500);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(500);
        builder.Property(x => x.NameTr).IsRequired().HasMaxLength(500);

        builder.Property(x => x.DescriptionAz).HasColumnType("text");
        builder.Property(x => x.DescriptionRu).HasColumnType("text");
        builder.Property(x => x.DescriptionEn).HasColumnType("text");
        builder.Property(x => x.DescriptionTr).HasColumnType("text");

        builder.Property(x => x.ShortDescriptionAz).HasMaxLength(1000);
        builder.Property(x => x.ShortDescriptionRu).HasMaxLength(1000);
        builder.Property(x => x.ShortDescriptionEn).HasMaxLength(1000);

        builder.Property(x => x.Barcode).HasMaxLength(100);
        
        // SKU
        builder.Property(x => x.Sku).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Sku).IsUnique();

        builder.Property(x => x.Slug).IsRequired().HasMaxLength(500);
        builder.HasIndex(x => x.Slug).IsUnique();

        // Pricing
        builder.Property(x => x.Price)
            .HasPrecision(10, 2)
            .IsRequired();
        

        builder.Property(x => x.CompareAtPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.CostPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.StockQuantity).HasDefaultValue(0);
        builder.Property(x => x.LowStockThreshold).HasDefaultValue(10);
        builder.Property(x => x.TrackInventory).HasDefaultValue(true);
        builder.Property(x => x.AllowBackorder).HasDefaultValue(false);



        
        builder.Property(x => x.MetaTitleAz).HasMaxLength(255);
        builder.Property(x => x.MetaTitleEn).HasMaxLength(255);
        builder.Property(x => x.MetaTitleRu).HasMaxLength(255);
        builder.Property(x => x.MetaTitleTr).HasMaxLength(255);
        builder.Property(x => x.MetaDescriptionAz).HasColumnType("text");
        builder.Property(x => x.MetaDescriptionEn).HasColumnType("text");
        builder.Property(x => x.MetaDescriptionRu).HasColumnType("text");
        builder.Property(x => x.MetaDescriptionTr).HasColumnType("text");
        // Flags
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsFeatured).HasDefaultValue(false);
        builder.Property(x => x.IsNew).HasDefaultValue(false);
        builder.Property(x => x.IsOnSale).HasDefaultValue(false);
        builder.Property(x => x.IsBestseller).HasDefaultValue(false);
  
        // Soft delete
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.Property(x => x.RatingAverage).HasPrecision(10, 2).HasDefaultValue(0.00);
        builder.Property(x => x.RatingCount).HasDefaultValue(0);
        builder.Property(x => x.ReviewCount).HasDefaultValue(0);
        builder.Property(x => x.OrderCount).HasDefaultValue(0);
        builder.Property(x => x.Currency).HasDefaultValue("AZN");
        
        builder.Property(x => x.ViewCount).HasDefaultValue(0);
        builder.Property(x => x.WishlistCount).HasDefaultValue(0);
        
        // Indexes
        builder.HasIndex(x => new { x.IsActive, x.PublishedAt });
        builder.HasIndex(x => x.Price);
        builder.HasIndex(x => new { x.RatingAverage, x.ReviewCount });
        builder.HasIndex(x => new { x.IsFeatured, x.IsActive });
        builder.HasIndex(x => new { x.IsNew, x.CreatedDate });
        builder.HasIndex(x => new { x.IsOnSale, x.Price });
        // Relationships
        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProductColors)
            .WithOne(c => c.Product)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProductCategories)
            .WithOne(pc => pc.Product)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
  
        builder.Property(x => x.LastUpdatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.DeletedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}

