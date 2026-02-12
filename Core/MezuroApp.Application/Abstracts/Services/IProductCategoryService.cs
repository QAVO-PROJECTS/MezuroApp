using MezuroApp.Application.Dtos.ProductCategory;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Abstracts.Services;

public interface IProductCategoryService
{
    Task AddProductCategory(ProductCategory productCategory);
    Task DeleteProductCategory(ProductCategory productCategory);
    Task<List<ProductCategoryDto>> GetAllProductCategories();
 
}