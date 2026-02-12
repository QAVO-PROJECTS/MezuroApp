namespace MezuroApp.Application.Dtos.Product;

public class UpdateProductImageMetaDto
{
    public string Id { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int ThumbnailIndex {get; set;}
    public int FileIndex {get; set;}
}