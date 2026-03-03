using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Category;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Http;
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
        private readonly IAuditLogService _audit;
        private readonly IHttpContextAccessor _http;

        public CategoryService(
            ICategoryReadRepository readRepo,
            ICategoryWriteRepository writeRepo,
            IProductReadRepository productReadRepo,
            IProductCategoryWriteRepository productCategoryWriteRepo,
            IProductCategoryReadRepository productCategoryReadRepo,
            IFileService fileService,
            IMapper mapper,
            IAuditLogService audit,
            IHttpContextAccessor http)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
            _productReadRepo = productReadRepo;
            _productCategoryWriteRepo = productCategoryWriteRepo;
            _productCategoryReadRepo = productCategoryReadRepo;
            _fileService = fileService;
            _mapper = mapper;
            _audit = audit;
            _http = http;
        }

        #region Queries

        public async Task<List<CategoryDto>> GetAllCategories()
        {
            var categories = await _readRepo.GetAllAsync(
                c =>   c.ParentId == null  && !c.IsDeleted,
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
        public async Task<List<CategoryDto>> GetFilteredCategoriesForActiveStatus(bool isActive)
        {
            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted &&  c.ParentId == null && c.IsActive==isActive,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                    .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }
        public async Task<List<CategoryDto>> GetFilteredCategoriesForShowMenu(bool isShowMenu)
        {
            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted &&  c.ParentId == null && c.IsActive==isShowMenu,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                    .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }
        public async Task<List<CategoryDto>> GetFilteredSubCategoriesForShowMenu(string parentId,bool isShowMenu)
        {
            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted &&  c.ParentId.ToString() == parentId && c.IsActive==isShowMenu,
                q => q
                    .Include(c => c.Children)
                    .Include(c => c.ProductCategories)
                    .ThenInclude(pc => pc.Product)
            );

            return _mapper.Map<List<CategoryDto>>(categories);
        }

        public async Task<List<CategoryDto>> GetFilteredSubCategoriesForActiveStatus(string parentId,bool isActive)
        {
            var categories = await _readRepo.GetAllAsync(
                c => !c.IsDeleted &&  c.ParentId.ToString() == parentId && c.IsActive==isActive,
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

            // base slug: adlardan götür (Slug gəlmirsə)
     
            // UNIQE slug
    

            var entity = _mapper.Map<Category>(dto);
            var baseSlug = Slugify(dto.NameEn ?? dto.NameAz ?? Guid.NewGuid().ToString("N"));
            entity.Slug = await EnsureUniqueSlugAsync(baseSlug);

            entity.ParentId = parentId;
            entity.Level = parent == null ? 1 : parent.Level + 1;


            entity.CreatedDate = UtcNow();
            entity.LastUpdatedDate = entity.CreatedDate;
            entity.IsDeleted = false;

            if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
                entity.ImageUrl = await _fileService.UploadFile(dto.ImageUrl, CategoryFolder);

            await _writeRepo.AddAsync(entity);
            await _writeRepo.CommitAsync();
            await WriteAuditAsync(
                action: "CREATE",
                entityType: "Categories",
                entityId: entity.Id,
                oldValues: null,
                newValues: CatSnap(entity)
            );
        }

        public async Task UpdateCategory(UpdateCategoryDto dto)
        {
            var gid = ParseGuidOrThrow(dto.Id, "Id");

            var entity = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted,
                q => q.Include(c => c.ProductCategories)
            ) ?? throw new GlobalAppException("Kateqoriya tapılmadı!");
            var oldSnap = CatSnap(entity);

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

            // 1) map (dto-dan gələn field-ları entity-ə yaz)
            _mapper.Map(dto, entity);

            // 2) parent/level
            if (dto.ParentId != null)
            {
                entity.ParentId = newParentId;
                entity.Level = parent == null ? 1 : parent.Level + 1;
                await RecalculateChildrenLevelsAsync(entity.Id, entity.Level + 1);
            }

            // 3) SLUG: dto-dan gəlmirsə -> NAME-lərdən generasiya et
            // NameEn/NameAz boşdursa, köhnə slug-u saxla
            var nameForSlug = entity.NameEn ?? entity.NameAz;

            if (!string.IsNullOrWhiteSpace(nameForSlug))
            {
                var baseSlug = Slugify(nameForSlug);
                entity.Slug = await EnsureUniqueSlugAsync(baseSlug, entity.Id);
            }
            // əks halda entity.Slug olduğu kimi qalır

            // 4) image
            if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
                entity.ImageUrl = await _fileService.UploadFile(dto.ImageUrl, CategoryFolder);

            entity.LastUpdatedDate = UtcNow();

            await _writeRepo.UpdateAsync(entity);
            await _writeRepo.CommitAsync();
            await WriteAuditAsync(
                action: "UPDATE",
                entityType: "Categories",
                entityId: entity.Id,
                oldValues: oldSnap,
                newValues: CatSnap(entity)
            );
        }
        

        public async Task DeleteCategory(string id)
        {
            var gid = ParseGuidOrThrow(id, "Id");
            var category = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted,
                q => q.Include(c => c.Children)
            ) ?? throw new GlobalAppException("Kateqoriya tapılmadı!");
            var oldSnap = CatSnap(category);

            await SoftDeleteCategoryTreeAsync(category);
            await _writeRepo.CommitAsync();
            // delete edəndən sonra entity-nin özü artıq deleted olacaq
            var newSnap = CatSnap(category);

            await WriteAuditAsync(
                action: "DELETE",
                entityType: "Categories",
                entityId: category.Id,
                oldValues: oldSnap,
                newValues: newSnap
            );
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
        private bool IsAdminRequest()
        {
            var user = _http.HttpContext?.User;
            if (user == null) return false;

            // Role-larını öz sisteminə görə düzəlt
            if (user.IsInRole("SuperAdmin") || user.IsInRole("Owner") || user.IsInRole("Admin"))
                return true;

            // və ya permission claim varsa:
            // məsələn Categories.Update, Categories.Read və s.
            return user.Claims.Any(c => c.Type == Permissions.ClaimType);
        }

        private string GetUserId()
        {
            var user = _http.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? user?.FindFirst("sub")?.Value
                   ?? "Anonymous";
        }
        private async Task WriteAuditAsync(
            string action,               // "CREATE" | "UPDATE" | "DELETE"
            string entityType,           // "Categories"
            Guid? entityId,
            Dictionary<string, object>? oldValues,
            Dictionary<string, object>? newValues)
        {
            if (!IsAdminRequest()) return;

            var (ip, ua) = GetRequestInfo();

            await _audit.LogAsync(new AuditLog
            {
                UserId = GetUserId(),
                Module = entityType,
                EntityId = entityId,
                ActionType = action,
                OldValuesJson  = oldValues ?? new Dictionary<string, object>(),
                NewValuesJson = newValues ?? new Dictionary<string, object>(),
                IpAddress = ip,
                UserAgent = ua,
                CreatedAt = DateTime.UtcNow
            });
        }
        private static Dictionary<string, object> CatSnap(Category c) => new()
        {
            ["id"] = c.Id.ToString(),
            ["parentId"] = c.ParentId?.ToString(),
            ["nameAz"] = c.NameAz,
            ["nameEn"] = c.NameEn,
            ["slug"] = c.Slug,
            ["isActive"] = c.IsActive,
            ["showInMenu"] = c.ShowInMenu,
            ["level"] = c.Level,
            ["imageUrl"] = c.ImageUrl,
            ["isDeleted"] = c.IsDeleted
        };

        private (string ip, string ua) GetRequestInfo()
        {
            var ctx = _http.HttpContext;
            var ip = ctx?.Connection.RemoteIpAddress?.ToString() ?? "";
            var ua = ctx?.Request.Headers["User-Agent"].ToString() ?? "";
            return (ip, ua);
        }

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
            var oldSnap = CatSnap(entity);

            update(entity);
            entity.LastUpdatedDate = DateTime.UtcNow;

            await _writeRepo.UpdateAsync(entity);
            await _writeRepo.CommitAsync();
            await WriteAuditAsync(
                action: "UPDATE",
                entityType: "Categories",
                entityId: entity.Id,
                oldValues: oldSnap,
                newValues: CatSnap(entity)
            );
        }
        private static DateTime UtcNow() => DateTime.UtcNow;

        private static string Slugify(string input)
        {
            // Sadə slugify: lower, trim, boşluqları tire et
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            s = string.Join("-", s.Split(new[] { ' ', '_', '/' }, StringSplitOptions.RemoveEmptyEntries));
            return s;
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid? currentId = null)
        {
            var slug = baseSlug;
            var i = 1;

            while (true)
            {
                var exists = await _readRepo.GetAsync(x =>
                    x.Slug == slug && (currentId == null || x.Id != currentId.Value)
                ); // <-- !x.IsDeleted YOX!

                if (exists == null)
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