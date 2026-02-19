using MezuroApp.Application.Dtos.WishlistItem;

namespace MezuroApp.Application.Abstracts.Services;

public interface IWishlistService
{
    Task ManageWishlistItemsAsync(string userId, string? productId = null, List<CreateWishlistItemDto>? products = null);
    Task<List<WishlistItemDto>> GetUserWishlistItemsAsync(string userId);
 
}