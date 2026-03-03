namespace MezuroApp.Application.Dtos.Admins;


public class AdminDto
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string PhoneNumber { get; set; }
    public bool IsSuperAdmin { get; set; }
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Permissions { get; set; } = Array.Empty<string>();
    
    public bool? IsActive { get; set; }
    public string? LastLoginAt { get; set; }
    public string? CreatedAt { get; set; }
}
