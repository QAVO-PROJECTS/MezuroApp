namespace MezuroApp.Application.Dtos.Product.ProductFilter;

public sealed class OptionFilterItemDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public List<OptionValueFilterItemDto> Values { get; set; } = new();
}