namespace MezuroApp.Application.Dtos.AdminUsers;

public sealed class AdminUserDetailDto
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool NewsletterSubscribed { get; set; }
    public string RegistrationDate { get; set; } = default!;

    // cards
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public string? LastOrderDate { get; set; }

    // table (orders)
    public List<AdminUserOrderListItemDto> Orders { get; set; } = new();
}