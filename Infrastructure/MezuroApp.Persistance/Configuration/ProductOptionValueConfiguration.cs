using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("ProductOptionValues");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ValueAz).HasMaxLength(150);
        builder.Property(v => v.ValueEn).HasMaxLength(150);
        builder.Property(v => v.ValueRu).HasMaxLength(150);
        builder.Property(v => v.ValueTr).HasMaxLength(150);

        builder.HasOne(v => v.Option)
            .WithMany(o => o.Values)
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VariantValues)
            .WithOne(vv => vv.OptionValue)
            .HasForeignKey(vv => vv.OptionValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}