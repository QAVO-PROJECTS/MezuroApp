using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class EmailCampaignLogConfiguration 
    : IEntityTypeConfiguration<EmailCampaignLog>
{
    public void Configure(EntityTypeBuilder<EmailCampaignLog> builder)
    {
        builder.ToTable("email_campaign_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .HasDefaultValue("pending");

        builder.Property(x => x.ExternalMessageId)
            .HasMaxLength(255);

        builder.Property(x => x.BounceType)
            .HasMaxLength(50);

        builder.HasOne(x => x.Campaign)
            .WithMany(c => c.Logs)
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Subscriber)
            .WithMany()
            .HasForeignKey(x => x.SubscriberId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}