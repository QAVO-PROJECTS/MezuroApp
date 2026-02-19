using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

public class CuponConfiguration : IEntityTypeConfiguration<Cupon>
{
    public void Configure(EntityTypeBuilder<Cupon> builder)
    {


        // PK
        builder.HasKey(x => x.Id);

        // Code (unique)
        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.Code)
            .IsUnique();

        // DiscountType
        builder.Property(x => x.DiscountType)
            .IsRequired()
            .HasMaxLength(20); // "percentage" | "fixed_amount"

        // DiscountValue
        builder.Property(x => x.DiscountValue)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        // Minimum Purchase
        builder.Property(x => x.MinimumPurchaseAmount)
            .HasColumnType("decimal(10,2)")
            .IsRequired(false);

        // MaxUses
        builder.Property(x => x.MaxUses)
            .IsRequired(false);

        // MaxUsesPerUser
        builder.Property(x => x.MaxUsesPerUser)
            .HasDefaultValue(1)
            .IsRequired();

        // CurrentUses
        builder.Property(x => x.CurrentUses)
            .HasDefaultValue(0)
            .IsRequired();

        // Validity
        builder.Property(x => x.ValidFrom)
            .IsRequired(false);

        builder.Property(x => x.ValidUntil)
            .IsRequired(false);

        // Active status
        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // Admin FK
        builder.Property(x => x.AdminId)
            .IsRequired();

        builder.HasOne(x => x.Admin)
            .WithMany()
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // Timestamps (BaseEntity)
        builder.Property(x => x.CreatedDate)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");


        
        builder.Property(x => x.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.LastUpdatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.DeletedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);
    }
}