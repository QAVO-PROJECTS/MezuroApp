namespace MezuroApp.Application.Dtos.Review;

public class SortReviewDto
{
    public string ProductId { get; set; }
    public bool? SortNewAndOld{ get; set; }
    public bool? SortLikeAndDislike{ get; set; }
    public bool? SortRating { get; set; }
}