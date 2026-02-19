using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;
public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }

    public Guid? ProductColorId { get; set; }
    public ProductColor ProductColor { get; set; }

    public decimal PriceModifier { get; set; }
    public decimal CompareAtPriceModifier { get; set; }
    public int StockQuantity { get; set; }
    public string? VariantSlug { get; set; }
    public string Sku { get; set; }
    public bool IsAvailable { get; set; }

    // 🔥 Multi-option dəstəyi
    public List<ProductVariantOptionValue> OptionValues { get; set; }
    public List<BasketItem>? BasketItems { get; set; }
}