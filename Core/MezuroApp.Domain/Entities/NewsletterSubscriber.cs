using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public sealed class NewsletterSubscriber : BaseEntity
{
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Subscription Status
    public bool IsActive { get; set; } = true;

    public string? SubscriptionSource { get; set; } 
    // 'website', 'checkout', 'popup', 'manual'

    // Preferences (JSONB)
    public string Preferences { get; set; } = "{\"newProducts\": true, \"promotions\": true, \"weeklyDigest\": false}";

    public string Frequency { get; set; } = "weekly"; 
    // 'daily', 'weekly', 'monthly'

    // Language preference
    public string PreferredLanguage { get; set; } = "az"; 
    // 'az', 'ru', 'en', 'tr'

    // Verification
    public bool IsVerified { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime? VerifiedAt { get; set; }

    // Unsubscribe
    public string? UnsubscribeToken { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public string? UnsubscribeReason { get; set; }

    // Linked User (if exists)
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    // Timestamps
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<EmailCampaignLog>? CampaignLogs { get; set; }
}