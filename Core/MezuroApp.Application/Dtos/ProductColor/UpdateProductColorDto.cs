namespace MezuroApp.Application.Dtos.ProductColor;

public class UpdateProductColorDto
{
    public string Id { get; set; }
    public string? ColorNameAz { get; set; }
    public string? ColorNameRu { get; set; }
    public string? ColorNameEn { get; set; } 
    public string? ColorNameTr { get; set; }
    
    public string? ColorCode { get; set; }
    public string? Sku { get; set; }

    public List<string>? NewColorImageIds { get; set; }
    public List<string>? DeletedColorImageIds { get; set; }
}