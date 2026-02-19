using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("order_items");

        b.HasKey(x => x.Id);

        b.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        b.Property(x => x.ProductId).HasColumnName("product_id").IsRequired();
        b.Property(x => x.ProductVariantId).HasColumnName("product_variant_id");

        // Snapshot
        b.Property(x => x.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(500).IsRequired();

        b.Property(x => x.ProductSku)
            .HasColumnName("product_sku")
            .HasMaxLength(100);

     
        b.Property(x => x.ProductVariantName).HasColumnName("variant_name").HasMaxLength(255);

        // Pricing
        b.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2).IsRequired();
        b.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();

        b.Property(x => x.SubTotal).HasColumnName("subtotal").HasPrecision(10, 2).IsRequired();
        b.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(10, 2).HasDefaultValue(0m);
        b.Property(x => x.Total).HasColumnName("total").HasPrecision(10, 2).IsRequired();

        b.Property(x => x.ProductImageUrl).HasColumnName("product_image_url").HasMaxLength(500);

        b.Property(x => x.CreatedDate)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        b.Property(x => x.LastUpdatedDate)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");     
        b.Property(x => x.DeletedDate)
            .HasColumnName("deleted_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP"); 

        // Relations
        b.HasOne(x => x.Order)
            .WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ProductVariant)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
