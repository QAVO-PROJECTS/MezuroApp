using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class BasketConfiguration : IEntityTypeConfiguration<Basket>
{
    public void Configure(EntityTypeBuilder<Basket> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.FootprintId)
            .HasMaxLength(128);

        // Unique index for UserId (PostgreSQL version)
        builder.HasIndex(b => b.UserId)
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");

        // Unique index for anonymous user footprint
        builder.HasIndex(b => b.FootprintId)
            .IsUnique()
            .HasFilter("\"FootprintId\" IS NOT NULL");

        // Ensure at least one of the fields is present
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_Basket_Owner",
                "\"UserId\" IS NOT NULL OR \"FootprintId\" IS NOT NULL");
        });

        builder.HasMany(b => b.BasketItems)
            .WithOne(bi => bi.Basket)
            .HasForeignKey(bi => bi.BasketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}