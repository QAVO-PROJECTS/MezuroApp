using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class ProductVariantOptionValue : BaseEntity
{
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; }

    public Guid OptionValueId { get; set; }
    public ProductOptionValue OptionValue { get; set; }
}