namespace MezuroApp.Application.Dtos.Product.ProductFilter;

public sealed class ColorFilterItemDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public int Count { get; set; }
}