using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        // Optional custom names
        builder.Property(o => o.CustomNameAz).HasMaxLength(150);
        builder.Property(o => o.CustomNameEn).HasMaxLength(150);
        builder.Property(o => o.CustomNameRu).HasMaxLength(150);
        builder.Property(o => o.CustomNameTr).HasMaxLength(150);

        // Product → ProductOption (1 → Many)
        builder.HasOne(o => o.Product)
            .WithMany(p => p.Options)
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // System-level Option → ProductOption (1 → Many)
        builder.HasOne(o => o.Option)
            .WithMany(s => s.ProductOptions)
            .HasForeignKey(o => o.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // ProductOption → ProductOptionValue (1 → Many)
        builder.HasMany(o => o.Values)
            .WithOne(v => v.Option)
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: a product cannot have the same option twice

    }
}
