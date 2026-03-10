using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.Product.ProductFilter;
using MezuroApp.Domain.HelperEntities;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(string id);
    Task<ProductDto> GetBySlugAsync(string slug);
    Task<List<ProductDto>> GetAllAsync();
    Task<PagedResult<ProductDto>> GetAllBestSellerAsync(int page, int pageSize);
    Task<PagedResult<ProductDto>> GetAllNewProductAsync(int page, int pageSize);
    Task<PagedResult<ProductDto>> GetAllOnSaleAsync(int page, int pageSize);
    Task<PagedResult<ProductDto>> GetAllProductForAdminAsync(int page, int pageSize);
    Task<PagedResult<ProductDto>> GetAllStatusFilteredProductForAdminAsync(int page, int pageSize, bool status);
    Task<PagedResult<ProductDto>> AdminSortedAsync(AdminProductSortRequestDto r);

    Task<PagedResult<ProductDto>> AdminSearchAsync(
        string term,
        string lang = "az",
        
        int page = 1,
        int pageSize = 20);

    Task<ProductFilterMetaDto> GetFilterMetaAsync(
        string? categoryId = null,
        string lang = "az");

    Task<PagedResult<ProductDto>> FilterAsync(ProductFilterRequestDto r);
    Task<PagedResult<ProductDto>> AdminFilterByPriceAsync(AdminProductFilterRequestDto r);
    Task<string> CreateAsync(CreateProductDto dto);
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
