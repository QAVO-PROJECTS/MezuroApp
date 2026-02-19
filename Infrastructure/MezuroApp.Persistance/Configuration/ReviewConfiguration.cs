using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Configuration;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {

        builder.HasKey(r => r.Id);

        // Indexes
        builder.HasIndex(r => r.ProductId);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedDate);

        // Properties
        builder.Property(r => r.Description)
       
               .HasMaxLength(2000);

        builder.Property(r => r.AdminReplyDescription)
               .HasMaxLength(2000);

        builder.Property(r => r.GuestName)
               .HasMaxLength(100);

        builder.Property(r => r.GuestSurname)
               .HasMaxLength(100);

        builder.Property(r => r.Rating)
               .HasPrecision(3, 2); // 0.00 - 9.99 (istəyə görə 2,1 də ola bilər)

        builder.Property(r => r.LikeCount)
               .HasDefaultValue(0);

        builder.Property(r => r.DislikeCount)
               .HasDefaultValue(0);

        builder.Property(r => r.Status)
               .HasDefaultValue(true);

        // BaseEntity fields
     

        // Relationships
        builder.HasOne(r => r.Product)
               .WithMany(p => p.Reviews)   // Product entitisində ICollection<Review> Reviews varsa
               .HasForeignKey(r => r.ProductId)
               .OnDelete(DeleteBehavior.Restrict); // Soft-delete ilə uyğundur

        builder.HasOne(r => r.User)
               .WithMany(u => u.Reviews)   // User entitisində ICollection<Review> Reviews varsa
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.SetNull); // User silinərsə rəylər qalır
        builder.Property(r => r.IsDeleted)
               .HasDefaultValue(false);
        builder.Property(x => x.CreatedDate)
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
  
        builder.Property(x => x.LastUpdatedDate)
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.DeletedDate)
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}