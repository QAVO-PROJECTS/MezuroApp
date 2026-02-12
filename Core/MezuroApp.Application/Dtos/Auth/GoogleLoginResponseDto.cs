namespace MezuroApp.Application.Dtos.Auth;

public class GoogleLoginResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public GoogleUserDto User { get; set; }

}
public class GoogleUserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string OAuthProvider { get; set; }
    public IList<string> Roles { get; set; }

}