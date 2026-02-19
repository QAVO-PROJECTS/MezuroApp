namespace MezuroApp.Application.Dtos.Product;

public class ProductImageDto
{
    public string? Id { get; set; }
    public string ImageUrl { get; set; }
    public string ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int? SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}