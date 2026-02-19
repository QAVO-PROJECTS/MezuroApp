namespace MezuroApp.Application.Dtos.Basket.BasketItem;

public class CreateBasketItemDto
{
    public string? ProductVariantId { get; set; }
    public string ProductId { get; set; }
    public int Quantity { get; set; }
}