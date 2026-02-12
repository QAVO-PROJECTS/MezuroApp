using MezuroApp.Application.Dtos.ProductOption;

namespace MezuroApp.Application.Abstracts.Services;

public interface IProductOptionService
{
    Task<ProductOptionDto> GetByIdAsync(string id);
    Task<List<ProductOptionDto>> GetByProductAsync(string productId);
    Task CreateAsync(CreateProductOptionDto dto);
    Task UpdateAsync(UpdateProductOptionDto dto);
    Task DeleteAsync(string id);
}