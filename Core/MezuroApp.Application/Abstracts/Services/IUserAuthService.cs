using MezuroApp.Application.Dtos.Auth;

namespace MezuroApp.Application.Abstracts.Services;

public interface IUserAuthService
{
    Task<RegisterResponseDto> Register(RegisterRequestDto registerRequestDto);
    Task<LoginResponseDto> Login(LoginRequestDto loginDto);
    Task<GoogleLoginResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto);
    Task SendResetPasswordLinkAsync(string email, string? resetPageBaseUrl = null);
    Task ResetPasswordAsync(string email, string token, string newPassword);
    Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
}