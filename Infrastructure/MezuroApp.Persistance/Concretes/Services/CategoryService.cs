using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Category;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MezuroApp.Persistance.Concretes.Services
{
    public class CategoryService : ICategoryService
    {
        private const string CategoryFolder = "categories";

        private readonly ICategoryReadRepository _readRepo;
        private readonly ICategoryWriteRepository _writeRepo;
        private readonly IProductReadRepository _productReadRepo;
        private readonly IProductCategoryWriteRepository _productCategoryWriteRepo;
        private readonly IProductCategoryReadRepository _productCategoryReadRepo;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;

        public CategoryService(
            ICategoryReadRepository readRepo,
            ICategoryWriteRepository writeRepo,
            IProductReadRepository productReadRepo,
            IProductCategoryWriteRepository productCategoryWriteRepo,
            IProductCategoryReadRepository productCategoryReadRepo,
            IFileService fileService,
            IMapper mapper)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
            _productReadRepo = productReadRepo;
            _productCategoryWriteRepo = productCategoryWriteRepo;
            _productCategoryReadRepo = productCategoryReadRepo;
            _fileService = fileService;
            _mapper = mapper;
        }

        #region Queries

        public async Task<List<CategoryDto>> GetAllCategories()
        {
            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted &&  c.ParentId == null ,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                        .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }
        public async Task<List<CategoryDto>> GetAllActiveCategories()
        {
            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted &&  c.ParentId == null && c.IsActive==true,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                    .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<List<CategoryDto>> GetAllMenuCategories()
        {
            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted &&  c.ParentId == null && c.IsActive==true && c.ShowInMenu==true,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                    .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }
        public async Task<List<CategoryDto>> GetAllCategoriesByParentId(string parentId)
        {
            var pid = ParseGuidOrThrow(parentId, "ParentId");

            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted && c.ParentId == pid && c.IsActive==true,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                        .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<List<CategoryDto>> GetAllMenuCategoriesByParentId(string parentId)
        {
            var pid = ParseGuidOrThrow(parentId, "ParentId");

            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted && c.ParentId == pid && c.IsActive==true && c.ShowInMenu==true,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                    .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }
        public async Task<CategoryDto?> GetCategoryById(string id)
        {
            var gid = ParseGuidOrThrow(id, "Id");

            var category = await _readRepo.GetAsync(
                c => c.Id == gid && !c.IsDeleted && c.IsActive==true,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                        .ThenInclude(pc => pc.Product)
            );

            return category == null ? null : _mapper.Map<CategoryDto>(category);
        }

        #endregion

        #region Commands

        public async Task CreateCategory(CreateCategoryDto dto)
        {
            Guid? parentId = null;
            Category? parent = null;

            if (!string.IsNullOrWhiteSpace(dto.ParentId))
            {
                parentId = ParseGuidOrThrow(dto.ParentId!, "ParentId");
                parent = await _readRepo.GetAsync(x => x.Id == parentId && !x.IsDeleted)
                         ?? throw new GlobalAppException("Parent kateqoriya tapılmadı!");
            }

            // SLUG UNIQE CONTROL
            string baseSlug;

            if (!string.IsNullOrWhiteSpace(dto.Slug))
            {
                baseSlug = Slugify(dto.Slug);

                var exists = await _readRepo.GetAsync(
                    x => !x.IsDeleted && x.Slug == baseSlug
                );

                if (exists != null)
                    throw new GlobalAppException("Bu slug artıq mövcuddur!");
            }
            else
            {
                baseSlug = Slugify(dto.NameEn ?? dto.NameAz ?? Guid.NewGuid().ToString("N"));
            }

            var entity = _mapper.Map<Category>(dto);

            entity.ParentId = parentId;
            entity.Level = parent == null ? 1 : parent.Level + 1;
            entity.Slug = baseSlug;

            entity.CreatedDate = UtcNow();
            entity.LastUpdatedDate = entity.CreatedDate;
            entity.IsDeleted = false;

            if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
                entity.ImageUrl = await _fileService.UploadFile(dto.ImageUrl, CategoryFolder);

            await _writeRepo.AddAsync(entity);
            await _writeRepo.CommitAsync();

        
        }

        public async Task UpdateCategory(UpdateCategoryDto dto)
        {
            var gid = ParseGuidOrThrow(dto.Id, "Id");

            var entity = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted,
                q => q.Include(c => c.ProductCategories)
            ) ?? throw new GlobalAppException("Kateqoriya tapılmadı!");

            Guid? newParentId = null;
            Category? parent = null;

            if (!string.IsNullOrWhiteSpace(dto.ParentId))
            {
                newParentId = ParseGuidOrThrow(dto.ParentId!, "ParentId");
                if (newParentId == gid)
                    throw new GlobalAppException("Kateqoriya özünü parent kimi təyin edə bilməz!");

                parent = await _readRepo.GetAsync(x => x.Id == newParentId && !x.IsDeleted)
                         ?? throw new GlobalAppException("Parent kateqoriya tapılmadı!");
            }

            // SLUG UNIQE CONTROL
            if (!string.IsNullOrWhiteSpace(dto.Slug))
            {
                var normalizedSlug = Slugify(dto.Slug);

                var exists = await _readRepo.GetAsync(
                    x => !x.IsDeleted && x.Slug == normalizedSlug
                );

                if (exists != null && exists.Id != entity.Id)
                    throw new GlobalAppException("Bu slug artıq mövcuddur!");

                entity.Slug = normalizedSlug;
            }

            // rest mapping
            _mapper.Map(dto, entity);

            if (dto.ParentId != null)
            {
                entity.ParentId = newParentId;
                entity.Level = parent == null ? 1 : parent.Level + 1;
                await RecalculateChildrenLevelsAsync(entity.Id, entity.Level + 1);
            }

            if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
                entity.ImageUrl = await _fileService.UploadFile(dto.ImageUrl, CategoryFolder);

            entity.LastUpdatedDate = UtcNow();
            await _writeRepo.UpdateAsync(entity);

       
        }
        

        public async Task DeleteCategory(string id)
        {
            var gid = ParseGuidOrThrow(id, "Id");
            var category = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted,
                q => q.Include(c => c.Children)
            ) ?? throw new GlobalAppException("Kateqoriya tapılmadı!");

            await SoftDeleteCategoryTreeAsync(category);
            await _writeRepo.CommitAsync();
        }

        public async Task DeleteAllCategoriesByParentId(string parentId)
        {
            var pid = ParseGuidOrThrow(parentId, "ParentId");
            var children = await _readRepo.GetAllAsync(
                x => !x.IsDeleted && x.ParentId == pid,
                q => q.Include(c => c.Children)
            );

            foreach (var child in children)
                await SoftDeleteCategoryTreeAsync(child);

            await _writeRepo.CommitAsync();
        }
        public async Task SetIsActiveAsync(string id, bool value)
            => await UpdateBooleanField(id, p => p.IsActive = value);
        public async Task SetIsShowMenuAsync(string id, bool value)
            => await UpdateBooleanField(id, p => p.ShowInMenu = value);

        #endregion

        #region Helpers

        private static Guid ParseGuidOrThrow(string id, string field)
        {
            if (!Guid.TryParse(id, out var gid))
                throw new GlobalAppException($"Yanlış {field} formatı!");
            return gid;
        }
        private static Guid ParseGuidOrThrow(string id)
        {
            if (!Guid.TryParse(id, out var gid))
                throw new GlobalAppException("Id format yanlışdır!");
            return gid;
        }
        private async Task UpdateBooleanField(string id, Action<Category> update)
        {
            var gid = ParseGuidOrThrow(id);

            var entity = await _readRepo.GetAsync(x => x.Id == gid && !x.IsDeleted)
                         ?? throw new GlobalAppException("Category not found!");

            update(entity);
            entity.LastUpdatedDate = DateTime.UtcNow;

            await _writeRepo.UpdateAsync(entity);
            await _writeRepo.CommitAsync();
        }
        private static DateTime UtcNow() => DateTime.UtcNow;

        private static string Slugify(string input)
        {
            // Sadə slugify: lower, trim, boşluqları tire et
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            s = string.Join("-", s.Split(new[] { ' ', '_', '/' }, StringSplitOptions.RemoveEmptyEntries));
            return s;
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? currentId)
        {
            var slug = baseSlug;
            var i = 1;

            while (true)
            {
                var existsEntity = await _readRepo.GetAsync(x =>
                    !x.IsDeleted &&
                    x.Slug == slug &&
                    (currentId == null || x.Id != currentId.Value)
                );

                if (existsEntity == null)
                    return slug;

                slug = $"{baseSlug}-{i++}";
            }
        }

        private async Task RecalculateChildrenLevelsAsync(Guid categoryId, int childLevel)
        {
            var children = await _readRepo.GetAllAsync(
                x => !x.IsDeleted && x.ParentId == categoryId
            );

            foreach (var child in children)
            {
                child.Level = childLevel;
                await _writeRepo.UpdateAsync(child);

                // Rekursiv
                await RecalculateChildrenLevelsAsync(child.Id, childLevel + 1);
            }
        }

        private async Task SoftDeleteCategoryTreeAsync(Category category)
        {
            // Özünü işarələ
            category.IsDeleted = true;
            category.DeletedDate = UtcNow();
            await _writeRepo.UpdateAsync(category);

            // Uşaqlar
            if (category.Children != null && category.Children.Any())
            {
                foreach (var child in category.Children.Where(c => !c.IsDeleted))
                {
                    var fullChild = await _readRepo.GetAsync(
                        x => x.Id == child.Id,
                        q => q.Include(c => c.Children)
                    );
                    if (fullChild != null)
                        await SoftDeleteCategoryTreeAsync(fullChild);
                }
            }
        }

        private async Task AttachProductsAsync(Guid categoryId, List<string> productIds)
        {
            var normalized = productIds
                .Where(x => Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

            if (normalized.Count == 0) return;

            // Mövcud linklər
            var category = await _readRepo.GetAsync(
                x => x.Id == categoryId,
                q => q.Include(c => c.ProductCategories)
            ) ?? throw new GlobalAppException("Kateqoriya tapılmadı!");

            var existingProductIds = (category.ProductCategories ?? new List<ProductCategory>())
                .Where(pc => !pc.IsDeleted)
                .Select(pc => pc.ProductId)
                .ToHashSet();

            var toAdd = normalized.Where(pid => !existingProductIds.Contains(pid)).ToList();
            if (toAdd.Count == 0) return;

            // Məhsulların mövcudluğunu yoxla
            var validProducts = await _productReadRepo.GetAllAsync(p => !p.IsDeleted && toAdd.Contains(p.Id));
            var validSet = validProducts.Select(p => p.Id).ToHashSet();

            foreach (var pid in toAdd.Where(x => validSet.Contains(x)))
            {
                var link = new ProductCategory
                {
                    Id = Guid.NewGuid(),
                    CategoryId = categoryId,
                    ProductId = pid,
                    CreatedDate = UtcNow(),
                    LastUpdatedDate = UtcNow(),
                    IsDeleted = false
                };
                await _productCategoryWriteRepo.AddAsync(link);
            }
        }

        private async Task DetachProductsAsync(Guid categoryId, List<string> productIds)
        {
            var normalized = productIds
                .Where(x => Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

            if (normalized.Count == 0) return;

            var links = await _productCategoryReadRepo.GetAllAsync(
                x => !x.IsDeleted && x.CategoryId == categoryId && normalized.Contains(x.ProductId)
            );

            foreach (var link in links)
            {
                // Soft delete
                link.IsDeleted = true;
                link.DeletedDate = UtcNow();
                await _productCategoryWriteRepo.UpdateAsync(link);

                // Tam silmək istəsən:
                // await _productCategoryWriteRepo.HardDeleteAsync(link);
            }
        }

        #endregion
    }
}