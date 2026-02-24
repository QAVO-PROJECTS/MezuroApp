namespace MezuroApp.Application.Dtos.EmailCampaigns;

public sealed class EmailCampaignDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string CampaignType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }

    public int TotalRecipients { get; set; }
    public int TotalSent { get; set; }
    public int TotalOpened { get; set; }
    public int TotalClicked { get; set; }
    public int TotalBounced { get; set; }
    public int TotalUnsubscribed { get; set; }
}