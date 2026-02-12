using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(IConfiguration configuration, UserManager<User> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"])
        );
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // ==== ROLES (Admin / SuperAdmin) ====
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        // ==== PERMISSIONS (ƏN VACİB HİSSƏ) ====
        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var c in userClaims.Where(c => c.Type == Permissions.ClaimType))
        {
            claims.Add(new Claim(Permissions.ClaimType, c.Value));
        }

        // Token 1 saatlıq olsun
        var expires = DateTime.UtcNow.AddHours(1);

        var securityToken = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: _signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }

    public async Task<string> GenerateRefreshTokenAsync(User user)
    {
        return Guid.NewGuid().ToString();
    }

    public async Task<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var validationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"])
                ),
                ValidateLifetime = false
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> RefreshAccessTokenAsync(string refreshToken)
    {
        var user = await GetUserFromRefreshToken(refreshToken);

        if (user == null)
            return null;

        return await GenerateAccessTokenAsync(user);
    }

    private async Task<User> GetUserFromRefreshToken(string refreshToken)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = "user@example.com",
            Email = "user@example.com"
        };
    }
}