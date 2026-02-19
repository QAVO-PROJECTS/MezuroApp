using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Application.Dtos.Auth;
using Microsoft.AspNetCore.Http;

namespace MezuroApp.Application.Abstracts.Services;

public interface IUserAuthService
{
    Task<RegisterResponseDto> Register(RegisterRequestDto registerRequestDto);
    Task EditProfileImage(string userId,IFormFile file);
    Task DeleteProfileImage(string userId);
    Task EditProfile(string userId,UpdateProfileDto updateProfileDto);
    
    Task<LoginResponseDto> Login(LoginRequestDto loginDto);
    Task<GoogleLoginResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto);
    Task SendResetPasswordLinkAsync(string email, string? resetPageBaseUrl = null);
    Task ResetPasswordAsync(string email, string token, string newPassword);
    Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
    Task<ProfileDto> GetProfile(string userId);
}