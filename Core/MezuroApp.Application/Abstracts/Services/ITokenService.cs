using System.Security.Claims;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Abstracts.Services;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user);
    Task<string> GenerateRefreshTokenAsync(User user);
    Task<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token);
    Task<string> RefreshAccessTokenAsync(string refreshToken);
}
