using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AbandonedCartConfiguration 
    : IEntityTypeConfiguration<AbandonedCart>
{
    public void Configure(EntityTypeBuilder<AbandonedCart> builder)
    {
        builder.ToTable("abandoned_carts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(255);

        builder.Property(x => x.CartItemsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.RecoveryEmailSent)
            .HasDefaultValue(false);
        builder.Property(x => x.Status).HasDefaultValue("created");

        builder.HasOne(x => x.User)
            .WithMany(x=>x.AbandonedCarts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ConvertedToOrder)
            .WithMany(x=>x.AbandonedCarts)
            .HasForeignKey(x => x.ConvertedToOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Basket)
            .WithMany(x=>x.AbandonedCarts)
            .HasForeignKey(x => x.BasketId)
            .OnDelete(DeleteBehavior.SetNull);

        // Performance üçün
        builder.HasIndex(x => x.FootprintId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt);
    }
} 