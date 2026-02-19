using MezuroApp.Application.Dtos.Basket;
using MezuroApp.Application.Dtos.Basket.BasketItem;

namespace MezuroApp.Application.Abstracts.Services;

public interface IBasketService
{
    Task AddOrUpdateBasketItemsForUserAsync(string userId, List<CreateBasketItemDto> itemsToAdd);
    Task AddOrUpdateBasketItemsForGuestAsync(string footprintId, List<CreateBasketItemDto> itemsToAdd);
    Task RemoveBasketItemForUserAsync(string userId, string productId, string? productVariantId);
    Task RemoveBasketItemForGuestAsync(string footprintId, string productId, string? productVariantId);
    Task RemoveAllBasketItemsForUserAsync(string userId);
    Task SetBasketItemQuantityAsync(string userId, string productId, string? productVariantId, int deltaQuantity);
    Task<BasketDto> GetBasketForUserAsync(string userId);
    Task<BasketDto> GetBasketForGuestAsync(string footprintId);
    Task MergeBasketAsync(string userId, string footprintId);




}