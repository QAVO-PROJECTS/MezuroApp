namespace MezuroApp.Application.Dtos.Auth;

public class LoginResponseDto
{

   public string AccessToken { get; set; }
   public string RefreshToken { get; set; }
   public int ExpiresIn { get; set; }
    public UserDto User { get; set; }

}

public class UserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public List<string> Roles { get; set; }

}