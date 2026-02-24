using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MezuroApp.Persistance.Configuration;

public sealed class NewsletterSubscriberConfiguration 
    : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.ToTable("newsletter_subscribers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .HasMaxLength(100);

        builder.Property(x => x.SubscriptionSource)
            .HasMaxLength(100);

        builder.Property(x => x.Preferences)
            .HasColumnType("jsonb");

        builder.Property(x => x.Frequency)
            .HasMaxLength(50)
            .HasDefaultValue("weekly");

        builder.Property(x => x.PreferredLanguage)
            .HasMaxLength(5)
            .HasDefaultValue("az");

        builder.Property(x => x.UnsubscribeToken)
            .HasMaxLength(255);

        builder.HasIndex(x => x.UnsubscribeToken)
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(x=>x.NewsletterSubscribers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}