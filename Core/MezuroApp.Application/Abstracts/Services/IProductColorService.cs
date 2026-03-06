using MezuroApp.Application.Dtos.ProductColor;

namespace MezuroApp.Application.Abstracts.Services;

public interface IProductColorService
{
    Task<ProductColorDto> GetByIdAsync(string id);
    Task<List<ProductColorDto>> GetAllAsync(string productId);
    Task<string> CreateAsync(CreateProductColorDto dto);
    Task UpdateAsync(UpdateProductColorDto dto);
    Task DeleteAsync(string id);
}