using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class ProductOption : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }

    public Guid OptionId { get; set; }   // Sistem Option-a istinad
    public Option Option { get; set; }

    // İstəyə görə məhsulda fərdi adları override etmək olar (optional)
    public string? CustomNameAz { get; set; }
    public string? CustomNameEn { get; set; }
    public string? CustomNameRu { get; set; }
    public string? CustomNameTr { get; set; }

    // Bu məhsula məxsus dəyərlər
    public List<ProductOptionValue> Values { get; set; }
}