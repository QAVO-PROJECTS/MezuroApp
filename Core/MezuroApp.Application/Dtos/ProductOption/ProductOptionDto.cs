using MezuroApp.Application.Dtos.Option;

namespace MezuroApp.Application.Dtos.ProductOption;

public class ProductOptionDto
{
    public string Id { get; set; } // ProductOption Id
 public string ProductId { get; set; }
    public string? CustomNameAz { get; set; }
    public string? CustomNameEn { get; set; }
    public string? CustomNameRu { get; set; }
    public string? CustomNameTr { get; set; }
    public string OptionNameAz { get; set; }
    public string OptionNameEn { get; set; }
    public string OptionNameRu { get; set; }
    public string OptionNameTr { get; set; }

    public List<UpdateProductOptionValueDto>? Values { get; set; }

}