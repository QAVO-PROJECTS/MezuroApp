namespace MezuroApp.Application.Dtos.EmailCampaigns;

public sealed class CreateEmailCampaignDto
{
    public string Name { get; set; } = default!;

    public string SubjectAz { get; set; } = default!;
    public string SubjectRu { get; set; } = default!;
    public string SubjectEn { get; set; } = default!;
    public string SubjectTr { get; set; } = default!;

    public string ContentAz { get; set; } = default!;
    public string ContentRu { get; set; } = default!;
    public string ContentEn { get; set; } = default!;
    public string ContentTr { get; set; } = default!;

    public string CampaignType { get; set; } = default!; // new_products, promotion...
    // ✅ UI üçün sabit
    // all_active_subscribers | verified_users
    public string TargetSegment { get; set; } = "all_active_subscribers";

    // draft/schedule toggle üçün
    public bool ScheduleForLater { get; set; }
    public string? ScheduledAtUtc { get; set; } // UI göndərəndə UTC göndər
}