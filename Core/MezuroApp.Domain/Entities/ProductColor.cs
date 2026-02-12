using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class ProductColor:BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    // Size i18n
    public string? ColorNameAz { get; set; }
    public string? ColorNameRu { get; set; }
    public string? ColorNameEn { get; set; } 
    public string? ColorNameTr { get; set; }
    
    public string? ColorCode { get; set; }
    public string? Sku { get; set; }
    public List<ProductVariant>? ColorVariants { get; set; }
    public List<ProductColorImage>? ColorImages { get; set; }
}