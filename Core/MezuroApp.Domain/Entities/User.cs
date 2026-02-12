using Microsoft.AspNetCore.Identity;

namespace MezuroApp.Domain.Entities;

public class User:IdentityUser<Guid>
{
  
    public string? Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string? OAuthProvider { get; set; }
    public string? OAuthProviderId { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpires { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }
    public string? TwoFactorSecret { get; set; }
   
    public bool? IsSubscribedToNewsletter { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<UserAddress>? UserAddresses { get; set; }
    public List<RefreshToken>? RefreshTokens { get; set; }
}