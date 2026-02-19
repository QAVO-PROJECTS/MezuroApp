using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Sku)
            .HasMaxLength(100);

        builder.HasIndex(pv => pv.Sku)
            .IsUnique(false);

        builder.Property(pv => pv.PriceModifier)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);
        builder.Property(pv => pv.CompareAtPriceModifier)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(pv => pv.StockQuantity)
            .HasDefaultValue(0);

        builder.Property(pv => pv.IsAvailable)
            .HasDefaultValue(true);

        builder.HasOne(pv => pv.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(pv => pv.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pv => pv.ProductColor)
            .WithMany(pc => pc.ColorVariants)
            .HasForeignKey(pv => pv.ProductColorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(pv => pv.OptionValues)
            .WithOne(ov => ov.Variant)
            .HasForeignKey(ov => ov.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
