namespace MezuroApp.Application.Dtos.ProductVariant
{
    public class ProductVariantDto
    {
        public string Id { get; set; }
        public string ProductId { get; set; }

        public string? ProductColorId { get; set; }
        public string? VariantSlug { get; set; }
        public string Sku { get; set; }

        public decimal PriceModifier { get; set; }
        public int StockQuantity { get; set; }
        public bool IsAvailable { get; set; }

        public List<ProductVariantOptionValueDto>? OptionValues { get; set; }
    }
}