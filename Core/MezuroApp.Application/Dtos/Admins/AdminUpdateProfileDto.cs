namespace MezuroApp.Application.Dtos.Admins;


public class AdminUpdateProfileDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
}
