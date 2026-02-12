namespace MezuroApp.Application.Dtos.ProductOption;

public class UpdateProductOptionValueDto
{
    public string? Id { get; set; }  // null → create
    public string? ValueAz { get; set; }
    public string? ValueEn { get; set; }
    public string? ValueRu { get; set; }
    public string? ValueTr { get; set; }
}