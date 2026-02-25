namespace MezuroApp.Application.Dtos.Product.ProductFilter;

public sealed class ProductFilterRequestDto
{
    public string? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public List<string>? ColorIds { get; set; }          // ProductColor.Id
    public List<string>? OptionValueIds { get; set; }    // ProductOptionValue.Id

    public string? Sort { get; set; } // newest | price_asc | price_desc | rating_desc | rating_asc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string Lang { get; set; } = "az";
}