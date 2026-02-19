using MezuroApp.Application.Dtos.Basket.BasketItem;

namespace MezuroApp.Application.Dtos.Basket;

public class BasketDto
{
    public string UserId { get; set; }
    public string FootprintId { get; set; }
    public List<BasketItemDto> BasketItems { get; set; }
    public decimal TotalAmount { get; set; }
    
}