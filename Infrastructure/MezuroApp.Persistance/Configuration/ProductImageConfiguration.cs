using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {


        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(x => x.ThumbnailUrl)
       
            .HasMaxLength(500);     
        builder.Property(x => x.AltText)

            .HasMaxLength(255);

        builder.Property(x=>x.SortOrder).HasDefaultValue(0);
        builder.Property(x=>x.IsPrimary).HasDefaultValue(false);
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(pi => pi.ColorImages)
            .WithOne(ci => ci.ProductImage)
            .HasForeignKey(ci => ci.ProductImageId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasIndex(x => new { x.ProductId, x.IsPrimary });


        builder.Property(x => x.CreatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
  
        builder.Property(x => x.LastUpdatedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(x => x.DeletedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
