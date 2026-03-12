using System.Security.Claims;
using MezuroApp.Application.Dtos.Auth;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Abstracts.Services;

public interface ITokenService
{   
    Task<string> GenerateAccessTokenAsync(User user);
    Task<string> GenerateRefreshTokenAsync(User user, string? ipAddress = null);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
    Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress = null);
}