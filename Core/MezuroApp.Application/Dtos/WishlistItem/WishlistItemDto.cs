using MezuroApp.Application.Dtos.Product;

namespace MezuroApp.Application.Dtos.WishlistItem;

public class WishlistItemDto
{
    
    public string WishlistId { get; set; }
    public ProductDto ProductDto { get; set; }
}