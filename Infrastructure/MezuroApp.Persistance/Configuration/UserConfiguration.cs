using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Primary Key
            builder.HasKey(u => u.Id);

            // Email Unique Constraint
            builder.HasIndex(u => u.NormalizedEmail).IsUnique();

            // User Identity Column Configuration
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
            builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(255);
            builder.Property(u => u.Username).HasMaxLength(100);
            builder.Property(u => u.PhoneNumber).HasMaxLength(20);

            // OAuth Provider
            builder.Property(u => u.OAuthProvider).HasMaxLength(50);
            builder.Property(u => u.OAuthProviderId).HasMaxLength(255);

            // Timestamps
            builder.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(u => u.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Soft Delete
            builder.Property(u => u.IsDeleted).HasDefaultValue(false);
            builder.Property(u => u.IsActive).HasDefaultValue(true);

            // Other fields
            builder.Property(u => u.FirstName).HasMaxLength(100);
            builder.Property(u => u.LastName).HasMaxLength(100);
            builder.Property(u => u.EmailConfirmationToken).HasMaxLength(500);
            builder.Property(u => u.PasswordResetToken).HasMaxLength(500);

            builder.Property(u => u.TwoFactorSecret).HasMaxLength(255);

            // Navigation Properties (Not mandatory for DB, but useful for ORM relationships)
            builder.HasMany(u => u.UserAddresses)
                .WithOne(ua => ua.User)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasDiscriminator<string>("UserType")
                .HasValue<User>("User")
                .HasValue<Admin>("Admin");

        }
    }
}
