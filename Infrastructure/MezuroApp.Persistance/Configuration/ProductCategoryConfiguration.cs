using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProductId, x.CategoryId })
            .IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ProductCategories)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.ProductCategories)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x=>x.SortOrder).HasDefaultValue(0);
        builder.Property(x=>x.IsPrimary).HasDefaultValue(false);

        builder.Property(x => x.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.LastUpdatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.DeletedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
