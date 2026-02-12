namespace MezuroApp.Application.Dtos.Admins;


public class AdminCreateRequestDto
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string PhoneNumber { get; set; }
    public IEnumerable<string>? InitialPermissions { get; set; } // istəyə görə ilkin icazələr
}
