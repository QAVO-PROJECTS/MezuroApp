
using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class Review:BaseEntity
{
    public string? Description { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    public string? AdminReplyDescription { get; set; }
    public DateTime? AdminReplyDate { get; set; }
    public string? GuestName  { get; set; }
    public string? GuestSurname{ get; set; }
    public decimal? Rating { get; set; }
    public int? LikeCount { get; set; }
    public int? DislikeCount { get; set; }
    public bool? Status { get; set; }
    




}