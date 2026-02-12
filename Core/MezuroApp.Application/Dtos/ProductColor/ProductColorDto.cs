using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.ProductVariant;

namespace MezuroApp.Application.Dtos.ProductColor;

public class ProductColorDto
{
    public string Id{ get; set; }
    public string? ColorNameAz { get; set; }
    public string? ColorNameRu { get; set; }
    public string? ColorNameEn { get; set; } 
    public string? ColorNameTr { get; set; }
    
    public string? ColorCode { get; set; }
    public string? Sku { get; set; }

    public List<ProductImageDto>? ColorImages { get; set; }
    public List<ProductVariantDto>? ColorVariants { get; set; }
}