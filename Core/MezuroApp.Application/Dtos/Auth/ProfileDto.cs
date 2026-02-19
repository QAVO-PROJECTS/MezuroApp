namespace MezuroApp.Application.Dtos.Auth;

public class ProfileDto
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string ProfileImageUrl { get; set; }
    public string BirthDate { get; set; }
}