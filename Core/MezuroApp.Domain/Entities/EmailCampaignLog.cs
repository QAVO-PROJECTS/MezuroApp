using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public sealed class EmailCampaignLog : BaseEntity
{
    public Guid CampaignId { get; set; }
    public EmailCampaign Campaign { get; set; } = default!;

    public Guid? SubscriberId { get; set; }
    public NewsletterSubscriber? Subscriber { get; set; }

    public string Email { get; set; } = default!;

    // Delivery Status
    public string Status { get; set; } = "pending";
    // 'pending', 'sent', 'delivered', 'bounced', 'failed'

    // Engagement
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }

    // External IDs
    public string? ExternalMessageId { get; set; } // SendGrid/Mailchimp message id

    // Error info
    public string? ErrorMessage { get; set; }
    public string? BounceType { get; set; }

    // Timestamps
    public DateTime? SentAt { get; set; }
}