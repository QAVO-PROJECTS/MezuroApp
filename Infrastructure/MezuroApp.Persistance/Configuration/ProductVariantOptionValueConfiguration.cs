using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class ProductVariantOptionValueConfiguration : IEntityTypeConfiguration<ProductVariantOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductVariantOptionValue> builder)
    {
        builder.ToTable("ProductVariantOptionValues");

        builder.HasKey(k => k.Id);

        builder.HasOne(vv => vv.Variant)
            .WithMany(v => v.OptionValues)
            .HasForeignKey(vv => vv.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vv => vv.OptionValue)
            .WithMany(ov => ov.VariantValues)
            .HasForeignKey(vv => vv.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}