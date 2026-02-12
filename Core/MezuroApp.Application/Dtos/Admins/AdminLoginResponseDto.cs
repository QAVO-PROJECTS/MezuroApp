namespace MezuroApp.Application.Dtos.Admins;


public class AdminLoginResponseDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresIn { get; set; }
    public AdminDto Admin { get; set; } = default!;
}
