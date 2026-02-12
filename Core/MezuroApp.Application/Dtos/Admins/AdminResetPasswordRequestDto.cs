namespace MezuroApp.Application.Dtos.Admins;


public class AdminResetPasswordRequestDto
{
    public string Email { get; set; } = default!;
    public string? ResetPageBaseUrl { get; set; } // admin panel üçün fərqli URL verilə bilər
}
