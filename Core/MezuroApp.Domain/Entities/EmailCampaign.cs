using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public sealed class EmailCampaign : BaseEntity
{
    // Campaign Info
    public string Name { get; set; } = default!;

    public string SubjectAz { get; set; } = default!;
    public string SubjectRu { get; set; } = default!;
    public string SubjectEn { get; set; } = default!;
    public string SubjectTr { get; set; } = default!;

    // Email Content
    public string ContentAz { get; set; } = default!;
    public string ContentRu { get; set; } = default!;
    public string ContentEn { get; set; } = default!;
    public string ContentTr { get; set; } = default!;

    // Campaign Type
    public string CampaignType { get; set; } = default!; 
    // 'new_products', 'promotion', 'abandoned_cart', 'price_drop', 'weekly_digest'

    // Targeting (JSONB)
    public string? TargetSegment { get; set; } // JSON string saxlanır (DB-də jsonb)

    // Status
    public string Status { get; set; } = "draft"; 
    // 'draft', 'scheduled', 'sending', 'sent', 'cancelled'

    // Scheduling
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }

    // Stats
    public int TotalRecipients { get; set; } = 0;
    public int TotalSent { get; set; } = 0;
    public int TotalOpened { get; set; } = 0;
    public int TotalClicked { get; set; } = 0;
    public int TotalBounced { get; set; } = 0;
    public int TotalUnsubscribed { get; set; } = 0;

    // Who created
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    // Navigation
    public List<EmailCampaignLog>? Logs { get; set; }
}