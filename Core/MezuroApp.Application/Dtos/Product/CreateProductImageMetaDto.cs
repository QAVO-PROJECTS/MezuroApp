namespace MezuroApp.Application.Dtos.Product;

public class CreateProductImageMetaDto
{
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int ThumbnailIndex {get; set;}
    public int FileIndex {get; set;}
    
  
}