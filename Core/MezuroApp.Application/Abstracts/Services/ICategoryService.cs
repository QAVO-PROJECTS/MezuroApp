using MezuroApp.Application.Dtos.Category;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Abstracts.Services;

public interface ICategoryService
{
   public Task<List<CategoryDto>> GetAllCategories();
   Task<List<CategoryDto>> GetAllMenuCategories();
   public Task<List<CategoryDto>> GetAllCategoriesByParentId(string parentId);
   Task<List<CategoryDto>> GetAllMenuCategoriesByParentId(string parentId);
   public Task<CategoryDto?> GetCategoryById(string id);
   Task<List<CategoryDto>> GetFilteredCategoriesForActiveStatus(bool isActive);
   Task<List<CategoryDto>> GetFilteredSubCategoriesForActiveStatus(string parentId, bool isActive);
   Task<List<CategoryDto>> GetFilteredCategoriesForShowMenu(bool isShowMenu);
   Task<List<CategoryDto>> GetFilteredSubCategoriesForShowMenu(string parentId, bool isShowMenu);
   Task<CategoryDto?> GetCategoryByIdForAdmin(string id);
   public Task CreateCategory(CreateCategoryDto categoryDto);
   public Task UpdateCategory(UpdateCategoryDto categoryDto);
   public Task DeleteCategory(string id);
   public Task DeleteAllCategoriesByParentId(string parentId);
   Task SetIsActiveAsync(string id, bool value);
   Task SetIsShowMenuAsync(string id, bool value);
   Task<List<CategoryDto>> GetAllActiveCategories();
}