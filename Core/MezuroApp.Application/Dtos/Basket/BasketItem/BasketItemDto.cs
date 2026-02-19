using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.ProductVariant;

namespace MezuroApp.Application.Dtos.Basket.BasketItem;

public class BasketItemDto
{
   public ProductVariantDto ProductVariant {get; set;}
   public ProductDto Product {get; set;}
    public decimal? Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? TotalCompareAtPrice { get; set; }
    public int Quantity { get; set; }
    public decimal FinalPrice { get; set; }
}