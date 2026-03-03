using MezuroApp.Application.Dtos.Auth;
using MezuroApp.Application.Dtos.Product;

namespace MezuroApp.Application.Dtos.Review;

public class ReviewDto
{
    public string Id { get; set; }
    public string Description { get; set; }

    public UserDto User  { get; set; }
    public ProductDto Product { get; set; }

    public string? AdminReplyDescription { get; set; }
    public string? AdminReplyDate { get; set; }
    public string? GuestName  { get; set; }
    public string? GuestSurname{ get; set; }
    public decimal? Rating { get; set; }
    public int? LikeCount { get; set; }
    public int? DislikeCount { get; set; }
    public bool? Status { get; set; }
    public bool? IsDeleted { get; set; }
    public string? CreatedDate { get; set; }
}