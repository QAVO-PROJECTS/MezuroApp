namespace MezuroApp.Application.Dtos.Product.ProductFilter;

public sealed class OptionValueFilterItemDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Count { get; set; }
}