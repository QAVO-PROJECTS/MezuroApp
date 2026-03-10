namespace MezuroApp.Application.Dtos.ProductOption;

public class  CreateProductOptionDto
{
    public string ProductId { get; set; }
    public string OptionId { get; set; }   // system-level Option

    public string? CustomNameAz { get; set; }
    public string? CustomNameEn { get; set; }
    public string? CustomNameRu { get; set; }
    
    
    
    
     
    public string? CustomNameTr { get; set; }

    public List<UpdateProductOptionValueDto>? Values { get; set; }
}
