namespace MezuroApp.Application.Dtos.ProductVariant
{
    public class CreateProductVariantDto
    {
        public string? ProductId { get; set; }
        public string? ProductColorId { get; set; }

        public string? VariantSlug { get; set; }    // boş gələrsə service özü yaradacaq
        public string? Sku { get; set; }

        public decimal PriceModifier { get; set; }
        public decimal CompareAtPriceModifier { get; set; }
        public int StockQuantity { get; set; }

        public List<string> OptionValueIds { get; set; } = new();
    }
}