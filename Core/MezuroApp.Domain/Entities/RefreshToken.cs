using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class RefreshToken:BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }    
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsExpired { get; set; }
    public bool IsActive { get; set; }
   
}