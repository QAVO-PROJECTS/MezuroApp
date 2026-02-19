using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");

        b.HasKey(x => x.Id);
        b.HasIndex(b => b.UserId)
     
            ;

        // Unique index for anonymous user footprint
        b.HasIndex(b => b.FootprintId)
   
            ;

        b.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .HasMaxLength(50)
            .IsRequired();

        b.HasIndex(x => x.OrderNumber).IsUnique();

        b.Property(x => x.UserId).HasColumnName("user_id");

        b.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        b.Property(x => x.Phone)
            .HasColumnName("phone_number")
            .HasMaxLength(20);

        b.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        b.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);

        // Shipping
        b.Property(x => x.ShippingAddressLineOne).HasColumnName("shipping_address_line1").HasMaxLength(500);
        b.Property(x => x.ShippingAddressLineTwo).HasColumnName("shipping_address_line2").HasMaxLength(500);
        b.Property(x => x.ShippingCity).HasColumnName("shipping_city").HasMaxLength(100);
        b.Property(x => x.ShippingState).HasColumnName("shipping_state").HasMaxLength(100);
        b.Property(x => x.ShippingPostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20);
        b.Property(x => x.ShippingCountry).HasColumnName("shipping_country").HasMaxLength(100);

        // Billing
        b.Property(x => x.BillingAddressLineOne).HasColumnName("billing_address_line1").HasMaxLength(500);
        b.Property(x => x.BillingAddressLineTwo).HasColumnName("billing_address_line2").HasMaxLength(500);
        b.Property(x => x.BillingCity).HasColumnName("billing_city").HasMaxLength(100);
        b.Property(x => x.BillingState).HasColumnName("billing_state").HasMaxLength(100);
        b.Property(x => x.BillingPostalCode).HasColumnName("billing_postal_code").HasMaxLength(20);
        b.Property(x => x.BillingCountry).HasColumnName("billing_country").HasMaxLength(100);

        // Status
        b.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasDefaultValue("pending");
        b.Property(x => x.PaymentStatus)
            .HasColumnName("payment_status")
            .HasMaxLength(50)
            .HasDefaultValue("pending");

        b.Property(x => x.FulfillmentStatus)
            .HasColumnName("fulfillment_status")
            .HasMaxLength(50)
            .HasDefaultValue("unfulfilled");

        // Pricing
        b.Property(x => x.SubTotal).HasColumnName("subtotal").HasPrecision(10, 2).IsRequired();
        b.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(10, 2).HasDefaultValue(0m);
        b.Property(x => x.ShippingCost).HasColumnName("shipping_cost").HasPrecision(10, 2).HasDefaultValue(0m);
        b.Property(x => x.TaxAmount).HasColumnName("tax_amount").HasPrecision(10, 2).HasDefaultValue(0m);
        b.Property(x => x.Total).HasColumnName("total").HasPrecision(10, 2).IsRequired();

        // Coupon / Delivery
        b.Property(x => x.CuponCode).HasColumnName("coupon_code").HasMaxLength(50);
        b.Property(x => x.DeliveryMethod).HasColumnName("delivery_method").HasMaxLength(100);
        b.Property(x => x.DeliveryNote).HasColumnName("delivery_notes");

        b.Property(x => x.EstimatedDeliveryDate).HasColumnName("estimated_delivery_date");
        b.Property(x => x.ActualDeliveryDate).HasColumnName("actual_delivery_date");

        b.Property(x => x.CreatedDate)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        b.Property(x => x.LastUpdatedDate)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");     
        b.Property(x => x.DeletedDate)
            .HasColumnName("deleted_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP"); 


        b.Property(x => x.ConfirmedDate).HasColumnName("confirmed_at");
        b.Property(x => x.ShippedDate).HasColumnName("shipped_at");
        b.Property(x => x.DeliveredDate).HasColumnName("delivered_at");
        b.Property(x => x.CancelledDate).HasColumnName("cancelled_at");

        b.Property(x => x.AdminNote).HasColumnName("admin_notes");

        // Relations
        b.HasOne(x => x.User)
            .WithMany(x=>x.Orders)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasMany(x => x.OrderItems)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
