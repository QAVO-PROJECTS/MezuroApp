namespace MezuroApp.Application.Dtos.ProductColor;

public class CreateProductColorDto
{
    public string ProductId { get; set; }

    // Size i18n
    public string? ColorNameAz { get; set; }
    public string? ColorNameRu { get; set; }
    public string? ColorNameEn { get; set; } 
    public string? ColorNameTr { get; set; }
    
    public string? ColorCode { get; set; }


    public List<string>? ColorImageIds { get; set; }
}