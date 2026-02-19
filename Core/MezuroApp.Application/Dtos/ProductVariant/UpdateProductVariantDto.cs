namespace MezuroApp.Application.Dtos.ProductVariant
{
    public class UpdateProductVariantDto
    {
        public string Id { get; set; }
        public string? ProductColorId { get; set; }

        public string? VariantSlug { get; set; }  // boş gələrsə slug yenidən generasiya ediləcək
        public string? Sku { get; set; }

        public decimal? PriceModifier { get; set; }
        public decimal? CompareAtPriceModifier { get; set; }
        public int? StockQuantity { get; set; }
        public bool? IsAvailable { get; set; }

        public List<string>? OptionValueIds { get; set; } // upsert list
    }
}