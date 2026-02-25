namespace MezuroApp.Application.Dtos.Product.ProductFilter;

public sealed class AdminProductFilterRequestDto
{
    public string? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string Lang { get; set; } = "az"; // az/en/ru/tr
}