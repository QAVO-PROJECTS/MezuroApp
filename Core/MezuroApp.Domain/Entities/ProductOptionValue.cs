using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class ProductOptionValue : BaseEntity
{
    public Guid OptionId { get; set; }
    public ProductOption Option { get; set; }

    public string ValueAz { get; set; }
    public string ValueRu { get; set; }
    public string ValueEn { get; set; }
    public string ValueTr { get; set; }

    public List<ProductVariantOptionValue> VariantValues { get; set; }
}
