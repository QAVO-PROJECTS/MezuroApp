using MezuroApp.Application.Dtos.Product;

namespace MezuroApp.Application.Dtos.ProductColor;

public class ProductColorShortDto
{
    public string Id{ get; set; }
    public string? ColorNameAz { get; set; }
    public string? ColorNameRu { get; set; }
    public string? ColorNameEn { get; set; } 
    public string? ColorNameTr { get; set; }
    
    public string? ColorCode { get; set; }
    public string? Sku { get; set; }

    public List<ProductImageDto>? ColorImages { get; set; }
}