namespace MezuroApp.Application.Dtos.Newsletter.Admin;

public sealed class AdminNewsletterSubscribersFilterDto
{
    public string? Search { get; set; }          // email
    public string? Status { get; set; }          // active | deactivated
    public string? Language { get; set; }        // az|en|ru|tr
    public string? Frequency { get; set; }       // daily|weekly|monthly

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}