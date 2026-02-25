namespace MezuroApp.Application.Dtos.Product.ProductFilter;

public sealed class AdminProductSortRequestDto
{
    public string? CategoryId { get; set; }
    public AdminProductSort Sort { get; set; } = AdminProductSort.DateDesc;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string Lang { get; set; } = "az";
}
public enum AdminProductSort
{
    NameAsc = 1,
    NameDesc = 2,
    DateAsc = 3,
    DateDesc = 4
}