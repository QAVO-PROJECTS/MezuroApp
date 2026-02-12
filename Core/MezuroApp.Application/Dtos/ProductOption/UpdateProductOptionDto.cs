namespace MezuroApp.Application.Dtos.ProductOption;

public class UpdateProductOptionDto
{
    public string Id { get; set; }

    public string? CustomNameAz { get; set; }
    public string? CustomNameEn { get; set; }
    public string? CustomNameRu { get; set; }
    public string? CustomNameTr { get; set; }

    public List<UpdateProductOptionValueDto>? Values { get; set; }
    public List<string>? DeleteValueIds { get; set; }
}