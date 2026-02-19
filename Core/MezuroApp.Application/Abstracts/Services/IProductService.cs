using MezuroApp.Application.Dtos.Product;
using MezuroApp.Domain.HelperEntities;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(string id);
    Task<List<ProductDto>> GetAllAsync();
    Task<PagedResult<ProductDto>> GetAllBestSellerAsync(int page, int pageSize);
    Task<PagedResult<ProductDto>> GetAllNewProductAsync(int page, int pageSize);
    Task<PagedResult<ProductDto>> GetAllOnSaleAsync(int page, int pageSize);
    Task CreateAsync(CreateProductDto dto);
    Task UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(string id);

    // Status methods
    Task SetIsActiveAsync(string id, bool value);
    Task SetIsFeaturedAsync(string id, bool value);
    Task SetIsNewAsync(string id, bool value);
    Task SetIsOnSaleAsync(string id, bool value);
    Task SetIsBestSellerAsync(string id, bool value);
    Task<PagedResult<ProductDto>> GetByCategoryAsync(string categoryId, int page, int pageSize);
}
