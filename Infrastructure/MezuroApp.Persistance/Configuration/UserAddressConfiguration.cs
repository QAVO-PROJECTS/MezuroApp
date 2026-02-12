using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Configuration
{
    public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
    {
        public void Configure(EntityTypeBuilder<UserAddress> builder)
        {
            // Primary Key
            builder.HasKey(ua => ua.Id);

            // Foreign Key Constraints
            builder.HasOne(ua => ua.User)
                .WithMany(u => u.UserAddresses)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Address Type
            builder.Property(ua => ua.AddressType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("shipping"); // Default value for AddressType

            // Properties
            builder.Property(ua => ua.AddressLine1)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(ua => ua.AddressLine2)
                .HasMaxLength(500);

            builder.Property(ua => ua.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ua => ua.StateProvince)
                .HasMaxLength(100);

            builder.Property(ua => ua.PostalCode)
                .HasMaxLength(20);

            builder.Property(ua => ua.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Azerbaijan");

            builder.Property(ua => ua.FullName)
                .HasMaxLength(200);

            builder.Property(ua => ua.IsDefault)
                .HasDefaultValue(false);

            // Timestamps
            builder.Property(ua => ua.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(ua => ua.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}