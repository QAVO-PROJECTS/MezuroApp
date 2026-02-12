using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Configuration
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Primary Key
            builder.HasKey(rt => rt.Id);

            // Foreign Key Constraints
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rt => rt.Token).IsUnique();

            // Properties
            builder.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
            builder.Property(rt => rt.CreatedByIp).HasMaxLength(50);
            builder.Property(rt => rt.ReplacedByToken).HasMaxLength(500);
// Computed Fields (with stored=true for PostgreSQL)
            builder.Property(rt => rt.IsExpired).HasDefaultValue(false);
               
            builder.Property(rt => rt.IsActive).HasDefaultValue(false);
                



            // Timestamps
            builder.Property(rt => rt.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}