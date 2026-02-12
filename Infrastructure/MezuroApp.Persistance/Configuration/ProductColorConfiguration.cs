using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

public class ProductColorConfiguration : IEntityTypeConfiguration<ProductColor>
{
    public void Configure(EntityTypeBuilder<ProductColor> builder)
    {
        builder.ToTable("ProductColors");

        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.ProductId).IsRequired();

        builder.Property(pc => pc.ColorNameAz).HasMaxLength(100);
        builder.Property(pc => pc.ColorNameRu).HasMaxLength(100);
        builder.Property(pc => pc.ColorNameEn).HasMaxLength(100);
        builder.Property(pc => pc.ColorNameTr).HasMaxLength(100);

        builder.Property(pc => pc.ColorCode).HasMaxLength(32);
        builder.Property(pc => pc.Sku).HasMaxLength(100);

        // Eyni məhsul daxilində ColorCode unikal olsun (əgər istəyirsinizsə)
        builder.HasIndex(pc => new { pc.ProductId, pc.ColorCode }).IsUnique(false);
        // SKU-nu ümumi unikal saxlamaq istəyirsinizsə:
        // builder.HasIndex(pc => pc.Sku).IsUnique();

        builder.HasOne(pc => pc.Product)
            .WithMany(p => p.ProductColors)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pc => pc.ColorVariants)
            .WithOne(v => v.ProductColor)
            .HasForeignKey(v => v.ProductColorId)
            .OnDelete(DeleteBehavior.SetNull); // Rəng silinərsə, variant qalır amma rəngsiz

        builder.HasMany(pc => pc.ColorImages)
            .WithOne(ci => ci.ProductColor)
            .HasForeignKey(ci => ci.ProductColorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}