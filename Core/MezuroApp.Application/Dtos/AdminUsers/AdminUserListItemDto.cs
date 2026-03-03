namespace MezuroApp.Application.Dtos.AdminUsers;

public sealed class AdminUserListItemDto
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool NewsletterSubscribed { get; set; }
    public string CreatedAt { get; set; } = default!;
}