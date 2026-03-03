namespace MezuroApp.Application.Dtos.AdminUsers;

public sealed class AdminUsersFilterDto
{
    public string? Search { get; set; }                 // email, name, phone
    public bool? NewsletterSubscribed { get; set; }      // true/false/null
    public bool? EmailConfirmed { get; set; }            // true/false/null
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
