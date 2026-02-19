using MezuroApp.Application.Dtos.Auth;

namespace MezuroApp.Application.Dtos.Review;

public class ReviewDto
{
    public string Id { get; set; }
    public string Description { get; set; }

    public string UserId { get; set; }
    public string ProductId { get; set; }

    public string? AdminReplyDescription { get; set; }
    public string? AdminReplyDate { get; set; }
    public string? GuestName  { get; set; }
    public string? UserName { get; set; }
    public string? UserSurname { get; set; }
    public string? UserProfileImage { get; set; }
    public string? GuestSurname{ get; set; }
    public decimal? Rating { get; set; }
    public int? LikeCount { get; set; }
    public int? DislikeCount { get; set; }
    public bool? Status { get; set; }
    public string? CreatedDate { get; set; }
}