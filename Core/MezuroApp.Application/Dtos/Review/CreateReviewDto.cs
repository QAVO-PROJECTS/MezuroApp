namespace MezuroApp.Application.Dtos.Review;

public class CreateReviewDto
{
    public string? Description { get; set; }
    public string? UserId { get; set; }

    public string ProductId { get; set; }
    public string? GuestName  { get; set; }
    public string? GuestSurname{ get; set; }
    public decimal? Rating { get; set; }
 
}