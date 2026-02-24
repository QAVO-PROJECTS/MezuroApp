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
    public string? TargetSegment { get; set; } // json string (optional)
}