using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(255);
        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.NameAz).IsRequired().HasMaxLength(255);
        builder.Property(x => x.NameRu).IsRequired().HasMaxLength(255);
        builder.Property(x => x.NameEn).IsRequired().HasMaxLength(255);
        builder.Property(x => x.NameTr).IsRequired().HasMaxLength(255);
        builder.Property(x => x.MetaTitleAz).HasMaxLength(255);
        builder.Property(x => x.MetaTitleEn).HasMaxLength(255);
        builder.Property(x => x.MetaTitleRu).HasMaxLength(255);
        builder.Property(x => x.MetaTitleTr).HasMaxLength(255);
        builder.Property(x => x.MetaDescriptionAz).HasColumnType("text");
        builder.Property(x => x.MetaDescriptionEn).HasColumnType("text");
        builder.Property(x => x.MetaDescriptionRu).HasColumnType("text");
        builder.Property(x => x.MetaDescriptionTr).HasColumnType("text");
        builder.Property(x => x.DescriptionAz).HasColumnType("text");
        builder.Property(x => x.DescriptionRu).HasColumnType("text");
        builder.Property(x => x.DescriptionEn).HasColumnType("text");
        builder.Property(x => x.DescriptionTr).HasColumnType("text");
        builder.Property(x => x.Level)
            .IsRequired().HasDefaultValue(1);

        builder.Property(x => x.ShowInMenu).HasDefaultValue(true);

        builder.Property(x => x.SortOrder).HasDefaultValue(0);
        // Timestamps
   builder.Property(x => x.CreatedDate)
              .HasDefaultValueSql("CURRENT_TIMESTAMP");
  
          builder.Property(x => x.LastUpdatedDate)
              .HasDefaultValueSql("CURRENT_TIMESTAMP");
          builder.Property(x => x.DeletedDate)
              .HasDefaultValueSql("CURRENT_TIMESTAMP");
      }     

   
}