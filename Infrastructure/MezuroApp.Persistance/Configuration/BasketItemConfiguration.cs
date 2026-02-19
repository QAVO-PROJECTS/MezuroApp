using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class BasketItemConfiguration : IEntityTypeConfiguration<BasketItem>
{
    public void Configure(EntityTypeBuilder<BasketItem> builder)
    {
        builder.HasKey(bi => bi.Id);

        builder.Property(bi => bi.BasketId).IsRequired();
        builder.Property(bi => bi.ProductVariantId);
        builder.Property(bi => bi.ProductId).IsRequired();
        builder.Property(bi => bi.Quantity).IsRequired();

        // Eyni variant eyni səbətdə təkrarlanmasın
        builder.HasIndex(bi => new { bi.BasketId, bi.ProductVariantId })
            .IsUnique();

        builder.HasOne(bi => bi.Basket)
            .WithMany(b => b.BasketItems)
            .HasForeignKey(bi => bi.BasketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Təklif: Restrict/NoAction — variant silinəndə səbət elementləri avtomatik silinməsin
        builder.HasOne(bi => bi.ProductVariant)
            .WithMany(pv => pv.BasketItems)
            .HasForeignKey(bi => bi.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(bi => bi.Product)
            .WithMany(pv => pv.Items)
            .HasForeignKey(bi => bi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}