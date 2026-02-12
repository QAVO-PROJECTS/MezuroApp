using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

public class ProductColorImageConfiguration : IEntityTypeConfiguration<ProductColorImage>
{
    public void Configure(EntityTypeBuilder<ProductColorImage> builder)
    {
        builder.ToTable("ProductColorImages");

        builder.HasKey(pci => pci.Id);

        builder.Property(pci => pci.ProductColorId).IsRequired();
        builder.Property(pci => pci.ProductImageId).IsRequired();

        // Eyni rəng-şəkil cütü təkrar olmasın
        builder.HasIndex(pci => new { pci.ProductColorId, pci.ProductImageId }).IsUnique();

        builder.HasOne(pci => pci.ProductColor)
            .WithMany(pc => pc.ColorImages)
            .HasForeignKey(pci => pci.ProductColorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pci => pci.ProductImage)
            .WithMany(pi => pi.ColorImages)
            .HasForeignKey(pci => pci.ProductImageId)
            .OnDelete(DeleteBehavior.Cascade);

        // TÖVSİYƏ: ProductColor.ProductId == ProductImage.ProductId zəmanəti lazımdır.
        // Bunu EF FK-ları ilə məcbur etmək olmur. Tətbiq səviyyəsində yoxlayın.
        // Əgər mütləq DB-level istəyirsinizsə, trigger və ya view + INSTEAD OF trigger istifadə edin.
    }
}