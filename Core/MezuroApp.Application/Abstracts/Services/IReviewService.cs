using MezuroApp.Application.Dtos.Review;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Application.Abstracts.Services;

public interface IReviewService
{
    Task<ReviewDto> GetByIdAsync(string id);
    Task<PagedResult<ReviewDto>> GetAllByProductAsync(string productId, int page = 1, int pageSize = 10);
    Task<PagedResult<ReviewDto>> GetAllInActiveAsync(int page = 1, int pageSize = 10);
    Task<PagedResult<ReviewDto>> GetAllForAdminAsync(int page = 1, int pageSize = 10);
    Task<PagedResult<ReviewDto>> GetAllInactiveByProductForAdminAsync(string productId, int page = 1, int pageSize = 10);
    Task<PagedResult<ReviewDto>> GetAllInactiveByRatingForAdminAsync(int rating, int page = 1, int pageSize = 10);
    Task<PagedResult<ReviewDto>> GetAllActiveByProductForAdminAsync(string productId, int page = 1, int pageSize = 10);
    Task<PagedResult<ReviewDto>> GetAllActiveByRatingForAdminAsync(int rating, int page = 1, int pageSize = 10);
    Task<PagedResult<ReviewDto>> GetAllInActiveSortedAsync(
        int sort = 2,
        int page = 1,
        int pageSize = 10);

    Task<PagedResult<ReviewDto>> GetAllActiveSortedAsync(
        int sort = 2,
        int page = 1,
        int pageSize = 10);

    Task<PagedResult<ReviewDto>> GetByStatusAndDeleteAsync(
        bool value,
        int page = 1,
        int pageSize = 10);
    Task InCreaseAsync(string reviewId);
    Task DeCrease(string reviewId);
    Task CreateAsync(CreateReviewDto dto);
    Task ReplyAsync(ReplyReviewDto dto);
    Task RejectAsync(string id);
    Task EditStatusAsync(string id, bool status);
    Task<List<ReviewDto>> SortReview(SortReviewDto dto);

}