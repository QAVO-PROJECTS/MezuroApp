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
   
    public string? ProfileImage { get; set; }
    public bool? IsSubscribedToNewsletter { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? NewsletterPreferences { get; set; } 
    public List<Review>? Reviews { get; set; }
    public List<UserAddress>? UserAddresses { get; set; }
    public List<RefreshToken>? RefreshTokens { get; set; }
    public Wishlist? Wishlist { get; set; }
    public List<Order>? Orders { get; set; }
    public List<EmailCampaign>? EmailCampaigns { get; set; }
    public List<AbandonedCart> ? AbandonedCarts { get; set; }
    public List<NewsletterSubscriber>? NewsletterSubscribers { get; set; }
}