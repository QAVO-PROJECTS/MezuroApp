using MezuroApp.Application.Dtos.Category;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Abstracts.Services;

public interface ICategoryService
{
   public Task<List<CategoryDto>> GetAllCategories();
   public Task<List<CategoryDto>> GetAllCategoriesByParentId(string parentId);
   public Task<CategoryDto?> GetCategoryById(string id);
   public Task CreateCategory(CreateCategoryDto categoryDto);
   public Task UpdateCategory(UpdateCategoryDto categoryDto);
   public Task DeleteCategory(string id);
   public Task DeleteAllCategoriesByParentId(string parentId);
}