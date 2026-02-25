namespace MezuroApp.Application.Dtos.Product.ProductFilter;

public sealed class ProductFilterMetaDto
{
    public List<ColorFilterItemDto> Colors { get; set; } = new();
    public List<OptionFilterItemDto> Options { get; set; } = new();
    public PriceRangeMetaDto PriceRange { get; set; } = new();
}