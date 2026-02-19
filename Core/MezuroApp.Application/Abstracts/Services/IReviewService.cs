using MezuroApp.Application.Dtos.Review;

namespace MezuroApp.Application.Abstracts.Services;

public interface IReviewService
{
    Task<ReviewDto> GetByIdAsync(string id);
    Task<List<ReviewDto>> GetAllAsync(string productId);
    Task<List<ReviewDto>> GetAllActiveAsync(string productId);
    Task InCreaseAsync(string reviewId);
    Task DeCrease(string reviewId);
    Task CreateAsync(CreateReviewDto dto);
    Task ReplyAsync(ReplyReviewDto dto);
    Task DeleteAsync(string id);
    Task EditStatusAsync(string id, bool status);
    Task<List<ReviewDto>> SortReview(SortReviewDto dto);
}