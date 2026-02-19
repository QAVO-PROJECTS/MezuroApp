using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.ProductColor;

namespace MezuroApp.Application.Dtos.ProductVariant
{
    public class ProductVariantDto
    {
        public string Id { get; set; }
      
        public ProductDto Product { get; set; }
        public ProductColorShortDto Color { get; set; }


        public string? VariantSlug { get; set; }
        public string Sku { get; set; }

        public decimal PriceModifier { get; set; }
        
        public decimal CompareAtPriceModifier { get; set; }
        public int StockQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public decimal Price { get; set; }
        public decimal CompareAtPrice { get; set; }
        public List<ProductVariantOptionValueDto>? OptionValues { get; set; }
    }
}