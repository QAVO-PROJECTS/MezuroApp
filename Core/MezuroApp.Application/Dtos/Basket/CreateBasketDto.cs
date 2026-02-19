using MezuroApp.Application.Dtos.Basket.BasketItem;

namespace MezuroApp.Application.Dtos.Basket;

public class CreateBasketDto
{
  public string? UserId { get; set; }
  public string? FootrintId { get; set; }

    public List<CreateBasketItemDto> Items { get; set; }
}