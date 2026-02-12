using MezuroApp.Application.Dtos.Product;
using MezuroApp.Domain.HelperEntities;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(string id);
    Task<List<ProductDto>> GetAllAsync();
    Task CreateAsync(CreateProductDto dto);
    Task UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(string id);

    // Status methods
    Task SetIsActiveAsync(string id, bool value);
    Task SetIsFeaturedAsync(string id, bool value);
    Task SetIsNewAsync(string id, bool value);
    Task SetIsOnSaleAsync(string id, bool value);
    Task<PagedResult<ProductDto>> GetByCategoryAsync(string categoryId, int page, int pageSize);
}
