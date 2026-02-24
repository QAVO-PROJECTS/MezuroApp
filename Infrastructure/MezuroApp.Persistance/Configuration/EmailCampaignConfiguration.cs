using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Persistance.Configuration;

public sealed class EmailCampaignConfiguration 
    : IEntityTypeConfiguration<EmailCampaign>
{
    public void Configure(EntityTypeBuilder<EmailCampaign> builder)
    {
        builder.ToTable("email_campaigns");

        builder.HasKey(x => x.Id);

        // Campaign info
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.SubjectAz)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.SubjectRu)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.SubjectEn)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.SubjectTr)
            .IsRequired()
            .HasMaxLength(255);

  
        builder.Property(x => x.TargetSegment)
            .IsRequired()
            .HasMaxLength(100);
        // HasColumnType("text") yazmaq da olar, yazmasan default varchar verir
        // Content
        builder.Property(x => x.ContentAz).IsRequired();
        builder.Property(x => x.ContentRu).IsRequired();
        builder.Property(x => x.ContentEn).IsRequired();
        builder.Property(x => x.ContentTr).IsRequired();

        builder.Property(x => x.CampaignType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .HasDefaultValue("draft");



        // Stats default
        builder.Property(x => x.TotalRecipients).HasDefaultValue(0);
        builder.Property(x => x.TotalSent).HasDefaultValue(0);
        builder.Property(x => x.TotalOpened).HasDefaultValue(0);
        builder.Property(x => x.TotalClicked).HasDefaultValue(0);
        builder.Property(x => x.TotalBounced).HasDefaultValue(0);
        builder.Property(x => x.TotalUnsubscribed).HasDefaultValue(0);

        // Relations
        builder.HasOne(x => x.CreatedBy)
            .WithMany(x=>x.EmailCampaigns)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
        
    }

}