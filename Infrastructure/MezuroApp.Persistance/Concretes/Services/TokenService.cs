using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MezuroApp.Application.Abstracts.Repositories.RefreshTokens;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Auth;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MezuroApp.Persistance.Concretes.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenReadRepository _refreshTokenReadRepository;
    private readonly IRefreshTokenWriteRepository _refreshTokenWriteRepository;
    private readonly SigningCredentials _signingCredentials;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _signingKey;

    public TokenService(
        IConfiguration configuration,
        UserManager<User> userManager,
        IRefreshTokenReadRepository refreshTokenReadRepository,
        IRefreshTokenWriteRepository refreshTokenWriteRepository)
    {
        _configuration = configuration;
        _userManager = userManager;
        _refreshTokenReadRepository = refreshTokenReadRepository;
        _refreshTokenWriteRepository = refreshTokenWriteRepository;

        _issuer = _configuration["Jwt:Issuer"] ?? throw new Exception("Jwt:Issuer tapılmadı");
        _audience = _configuration["Jwt:Audience"] ?? throw new Exception("Jwt:Audience tapılmadı");
        _signingKey = _configuration["Jwt:SigningKey"] ?? throw new Exception("Jwt:SigningKey tapılmadı");

   

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
//
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var c in userClaims.Where(c => c.Type == Permissions.ClaimType))
        {
            claims.Add(new Claim(Permissions.ClaimType, c.Value));
        }

        var expires = DateTime.UtcNow.AddHours(1);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: _signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(User user, string? ipAddress = null)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var entity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = ipAddress
        };

        await _refreshTokenWriteRepository.AddAsync(entity);
        await _refreshTokenWriteRepository.CommitAsync();

        return refreshToken;
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token algoritmi etibarsızdır.");
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var tokenEntity = await _refreshTokenReadRepository
            .GetAsync(x => x.Token == refreshToken)
            ;

        if (tokenEntity == null)
            throw new GlobalAppException("Refresh token tapılmadı.");

        if (!tokenEntity.IsActive)
            throw new GlobalAppException("Refresh token artıq etibarlı deyil.");

        var user = await _userManager.FindByIdAsync(tokenEntity.UserId.ToString());
        if (user == null)
            throw new GlobalAppException("İstifadəçi tapılmadı.");

        var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.RevokedByIp = ipAddress;
        tokenEntity.ReplacedByToken = newRefreshToken;

        await _refreshTokenWriteRepository.UpdateAsync(tokenEntity);

        var newTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = ipAddress
        };

        await _refreshTokenWriteRepository.AddAsync(newTokenEntity);
        await _refreshTokenWriteRepository.CommitAsync();

        var accessToken = await GenerateAccessTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600,
            User = new UserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            }
        };
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var tokenEntity = await _refreshTokenReadRepository
            .GetAsync(x => x.Token == refreshToken)
          ;

        if (tokenEntity == null)
            throw new GlobalAppException("Refresh token tapılmadı.");

        if (!tokenEntity.IsActive)
            throw new GlobalAppException("Refresh token artıq deaktivdir.");

        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.RevokedByIp = ipAddress;

        await _refreshTokenWriteRepository.UpdateAsync(tokenEntity);
        await _refreshTokenWriteRepository.CommitAsync();
    }
}