using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class OptionConfiguration : IEntityTypeConfiguration<Option>
{
    public void Configure(EntityTypeBuilder<Option> builder)
    {
        builder.Property(o => o.NameAz).HasMaxLength(150).IsRequired();
        builder.Property(o => o.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(o => o.NameRu).HasMaxLength(150).IsRequired();
        builder.Property(o => o.NameTr).HasMaxLength(150).IsRequired();

        // One system option → many product options
        builder.HasMany(o => o.ProductOptions)
            .WithOne(po => po.Option)
            .HasForeignKey(po => po.OptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint (system-level options must have unique names)
        builder.HasIndex(o => o.NameAz).IsUnique();
    }
}