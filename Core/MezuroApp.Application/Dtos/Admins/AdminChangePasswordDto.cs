namespace MezuroApp.Application.Dtos.Admins;


public class AdminChangePasswordDto
{
    public string OldPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
