using System.Globalization;
using System.Text;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Repositories.WishlistItems;
using MezuroApp.Application.Abstracts.Repositories.Wishlists;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

public class ProductService : IProductService
{
    private readonly IProductReadRepository _readRepo;
    private readonly IProductWriteRepository _writeRepo;
    private readonly IProductCategoryWriteRepository _categoryWriteRepo;
    private readonly IProductCategoryReadRepository _categoryReadRepo;
    private readonly IFileService _fileService;
    private readonly IWishlistItemReadRepository _wishlistReadRepo;
    private readonly IMapper _mapper;
    private readonly IEmailCampaignService _campaignService;

    private const string ProductFolder = "products";

    public ProductService(
        IProductReadRepository readRepo,
        IProductWriteRepository writeRepo,
        IProductCategoryWriteRepository categoryWriteRepo,
        IProductCategoryReadRepository categoryReadRepo,
        IFileService fileService,
        IWishlistItemReadRepository wishlistReadRepo,
        IEmailCampaignService campaignService,
        IMapper mapper)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _categoryWriteRepo = categoryWriteRepo;
        _categoryReadRepo = categoryReadRepo;
        _fileService = fileService;
        _wishlistReadRepo = wishlistReadRepo;
        _mapper = mapper;
        _campaignService = campaignService;
    }

    // ================================================
    //                GET METHODS
    // ================================================
    public async Task<ProductDto> GetByIdAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted && x.IsActive == true,
            q => q
                .Include(p => p.Images.Where(i => !i.IsDeleted))
                .Include(p => p.ProductCategories.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages)
                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)
                .Include(p => p.Reviews) // istəsən: .Where(r => !r.IsDeleted && r.Status == true)
            ,
            enableTracking: true // <-- vacib: artımı save etmək üçün
        );

        if (entity == null)
            throw new GlobalAppException("PRODUCT_NOT_FOUND"); // lüğətdə açar varsa onu istifadə et

        var items = await _wishlistReadRepo.GetAllAsync(x => x.ProductId == entity.Id);
        // ViewCount nullable-dırsa null-coalescing istifadə et
        var wishlistCount = await _wishlistReadRepo.GetCountAsync(x => x.ProductId.ToString() == id);


        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();

        var dto = _mapper.Map<ProductDto>(entity);
        dto.WishlistCount = wishlistCount;
        if (dto.Images != null)
            dto.Images = dto.Images
                .OrderBy(i => i.SortOrder ?? int.MaxValue)
                .ToList();

        return dto;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var entities = await _readRepo.GetAllAsync(
            x => !x.IsDeleted || x.IsDeleted,
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages) // filter YOX
                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)
                .Include(p => p.Reviews)
        );
        var list = _mapper.Map<List<ProductDto>>(entities);

        foreach (var p in list)
        {
            if (p.Images != null)
                p.Images = p.Images.OrderBy(i => i.SortOrder ?? int.MaxValue).ToList();
        }

        return list;
    }

    // ================================================
    //                CREATE
    // ================================================
    public async Task CreateAsync(CreateProductDto dto)
    {
        // 1) Map Product basic fields
        var entity = _mapper.Map<Product>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedDate = DateTime.UtcNow;
        entity.LastUpdatedDate = entity.CreatedDate;
        entity.IsDeleted = false;


        var baseName = FirstNonEmpty(dto.NameEn, dto.NameAz, dto.NameTr, dto.NameRu)
                       ?? $"PRD-{entity.Id.ToString("N")[..6]}";

        // 2) SKU & Slug (auto-generate əgər boş və ya null gəlirsə)
        entity.Sku = string.IsNullOrWhiteSpace(dto.Sku) ? await GenerateUniqueSkuAsync(baseName) : dto.Sku!.Trim();
        entity.Slug = string.IsNullOrWhiteSpace(dto.Slug)
            ? await GenerateUniqueSlugAsync(baseName)
            : Slugify(dto.Slug!.Trim());

        // 3) Unikal yoxla (əlavə təhlükəsizlik)
        await EnsureSkuSlugUniqueAsync(entity.Sku, entity.Slug, null);

        // Əgər PublishedAt gəlməyibsə və IsActive true-dursa → indi yazılır
        if (entity.IsActive == true && entity.PublishedAt == null)
            entity.PublishedAt = DateTime.UtcNow;

        // 2) SKU & Slug unikal yoxla


        // 3) Şəkil yükləmə (metadata-driven, mismatch-safe)

        // 3) Şəkil yükləmə (front thumbnail göndərmir)
        var imageEntities = new List<ProductImage>();

        var imageFiles = dto.ImageFiles ?? new List<IFormFile>();
        var metas = dto.Images ?? new List<CreateProductImageMetaDto>();

        var usedIdx = new HashSet<int>();

        if (metas.Count > 0)
        {
            foreach (var meta in metas.OrderBy(m => m.SortOrder ?? m.FileIndex))
            {
                if (meta.FileIndex < 0 || meta.FileIndex >= imageFiles.Count) continue;
                if (usedIdx.Contains(meta.FileIndex)) continue;

                var file = imageFiles[meta.FileIndex];
                var imageUrl = await _fileService.UploadFile(file, ProductFolder);

                string? thumbUrl = null;
                if (meta.IsPrimary)
                    thumbUrl = await CreateAndUploadThumbnailAsync(file, ProductFolder);

                imageEntities.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = thumbUrl,
                    AltText = meta.AltText ?? "",
                    IsPrimary = meta.IsPrimary,
                    SortOrder = meta.SortOrder ?? imageEntities.Count
                });

                usedIdx.Add(meta.FileIndex);
            }

            // metada olmayan qalan şəkilləri də əlavə etmək istəyirsənsə:
            for (int i = 0; i < imageFiles.Count; i++)
            {
                if (usedIdx.Contains(i)) continue;

                var file = imageFiles[i];
                var imageUrl = await _fileService.UploadFile(file, ProductFolder);

                imageEntities.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = null,
                    AltText = "",
                    IsPrimary = false,
                    SortOrder = imageEntities.Count
                });
            }
        }
        else
        {
            // meta yoxdursa: 1-ci şəkil primary olsun + thumbnail yaransın
            for (int i = 0; i < imageFiles.Count; i++)
            {
                var file = imageFiles[i];
                var imageUrl = await _fileService.UploadFile(file, ProductFolder);

                var isPrimary = (i == 0);
                string? thumbUrl = null;
                if (isPrimary)
                    thumbUrl = await CreateAndUploadThumbnailAsync(file, ProductFolder);

                imageEntities.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = thumbUrl,
                    AltText = "",
                    IsPrimary = isPrimary,
                    SortOrder = i
                });
            }
        }

// Primary yoxdursa -> birincini primary et + thumbnail yarat
        if (imageEntities.Count > 0 && !imageEntities.Any(x => x.IsPrimary))
        {
            imageEntities[0].IsPrimary = true;
            // imageEntities[0] üçün thumbnail yoxdursa, yaratmaq üçün orijinal file stream lazım olur.
            // Meta yoxdursa yuxarıda artıq yaradılıb. Meta varsa və heç biri primary deyildisə,
            // burada thumbnail yaratmaq üçün file əlimizdə olmadığına görə bunu etməyəcəyik.
        }

        entity.Images = imageEntities;


        // Heç bir şəkil IsPrimary deyilsə → birincini primary et
        if (imageEntities.Count > 0 && !imageEntities.Any(x => x.IsPrimary))
            imageEntities[0].IsPrimary = true;

        entity.Images = imageEntities;

        // 4) Save Product
        await _writeRepo.AddAsync(entity);
       
        await _writeRepo.CommitAsync();
    

        // 5) Category Relations
        if (dto.ProductCategoryIds != null)
        {
            foreach (var cid in dto.ProductCategoryIds)
            {
                if (!Guid.TryParse(cid, out var guid)) continue;

                await _categoryWriteRepo.AddAsync(new ProductCategory
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    CategoryId = guid,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            await _writeRepo.CommitAsync();
        }
        if (entity.IsActive == true)
        {
            await _campaignService.CreateAndScheduleNewProductCampaignAsync(entity);
        }
    }

    // ================================================
    //                UPDATE
    // ================================================
    public async Task UpdateAsync(UpdateProductDto dto)
    {
        var gid = ParseGuidOrThrow(dto.Id);

        // ======================================
        // 1) PRODUCT-u TRACKING rejimində gətir
        // ======================================
        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q
                .Include(p => p.Images.Where(i => !i.IsDeleted))
                .Include(p => p.ProductCategories.Where(pc => !pc.IsDeleted)),
            enableTracking: true
        ) ?? throw new GlobalAppException("PRODUCT_NOT_FOUND");


        // ======================================
        // 2) SKU və Slug unikallığı
        // ======================================
        await EnsureSkuSlugUniqueAsync(
            dto.Sku ?? entity.Sku,
            dto.Slug ?? entity.Slug,
            entity.Id
        );

        // ======================================
        // 3) Property-lərin update olunması
        // ======================================
        _mapper.Map(dto, entity);


        var baseName = FirstNonEmpty(entity.NameEn, entity.NameAz, entity.NameTr, entity.NameRu)
                       ?? $"PRD-{entity.Id.ToString("N")[..6]}";

        if (dto.Sku != null)
        {
            var trimmed = dto.Sku.Trim();
            entity.Sku = string.IsNullOrWhiteSpace(trimmed)
                ? await GenerateUniqueSkuAsync(baseName)
                : trimmed;
        }


        if (dto.Slug != null)
        {
            var trimmed = dto.Slug.Trim();
            entity.Slug = string.IsNullOrWhiteSpace(trimmed)
                ? await GenerateUniqueSlugAsync(baseName)
                : Slugify(trimmed);
        }

        await EnsureSkuSlugUniqueAsync(entity.Sku, entity.Slug, entity.Id);


        entity.LastUpdatedDate = DateTime.UtcNow;


        // =====================================================
        // 4) IMAGE DELETE — silinməli şəkilləri işarələ
        // =====================================================
        if (dto.DeleteImageIds != null)
        {
            var removeIds = dto.DeleteImageIds
                .Where(x => Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .ToList();

            var toRemove = entity.Images
                .Where(img => removeIds.Contains(img.Id))
                .ToList();

            foreach (var img in toRemove)
            {
                img.IsDeleted = true;
                img.DeletedDate = DateTime.UtcNow;
            }
        }


        // =====================================================
        // 5) IMAGE UPSERT (ID-null => CREATE, ID-not-null => UPDATE)
        // =====================================================

        var imageFiles = dto.NewImageFiles ?? new List<IFormFile>();
        var metaList  = dto.NewImages ?? new List<UpdateProductImageMetaDto>();

        foreach (var meta in metaList.OrderBy(m => m.SortOrder ?? m.FileIndex))
        {
            bool isNew = string.IsNullOrWhiteSpace(meta.Id);

            if (isNew)
            {
                if (meta.FileIndex < 0 || meta.FileIndex >= imageFiles.Count) continue;

                var imgFile = imageFiles[meta.FileIndex];
                var imgUrl = await _fileService.UploadFile(imgFile, ProductFolder);

                string? thumbUrl = null;
                if (meta.IsPrimary)
                    thumbUrl = await CreateAndUploadThumbnailAsync(imgFile, ProductFolder);

                entity.Images.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imgUrl,
                    ThumbnailUrl = thumbUrl,
                    AltText = meta.AltText ?? "",
                    IsPrimary = meta.IsPrimary,
                    SortOrder = meta.SortOrder ?? entity.Images.Count
                });

                continue;
            }

            // mövcud image
            var imgEntity = entity.Images.FirstOrDefault(x => x.Id.ToString() == meta.Id && !x.IsDeleted);
            if (imgEntity == null) continue;

            // şəkil dəyişdirilibsə
            if (meta.FileIndex >= 0 && meta.FileIndex < imageFiles.Count)
            {
                var newImgFile = imageFiles[meta.FileIndex];
                imgEntity.ImageUrl = await _fileService.UploadFile(newImgFile, ProductFolder);

                // Əgər bu image primary olacaqsa -> thumbnail yenilə
                if (meta.IsPrimary)
                    imgEntity.ThumbnailUrl = await CreateAndUploadThumbnailAsync(newImgFile, ProductFolder);
            }

            imgEntity.AltText = meta.AltText ?? imgEntity.AltText;
            imgEntity.IsPrimary = meta.IsPrimary;
            imgEntity.SortOrder = meta.SortOrder ?? imgEntity.SortOrder;
            imgEntity.LastUpdatedDate = DateTime.UtcNow;
        }


  


        // =====================================================
        // 6) Primary fix — ən azı 1 şəkil primary qalsın
        // =====================================================
        var activeImgs = entity.Images.Where(x => !x.IsDeleted).ToList();
        var primaries = activeImgs.Where(x => x.IsPrimary).ToList();

        if (primaries.Count == 0 && activeImgs.Count > 0)
            activeImgs.First().IsPrimary = true;
        else if (primaries.Count > 1)
        {
            foreach (var extra in primaries.Skip(1))
                extra.IsPrimary = false;
        }


        // =====================================================
        // 7) CATEGORY UPDATE (Add & Remove)
        // =====================================================

        // Add categories
        if (dto.NewProductCategoryIds != null)
        {
            foreach (var cid in dto.NewProductCategoryIds)
            {
                if (!Guid.TryParse(cid, out var guid)) continue;


                bool exists = entity.ProductCategories
                    .Any(pc => pc.CategoryId == guid && !pc.IsDeleted);


                if (!exists)
                {
                    var category = new ProductCategory
                    {
                        Id = Guid.NewGuid(),
                        ProductId = entity.Id,
                        CategoryId = guid,
                        CreatedDate = DateTime.UtcNow,
                        LastUpdatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    await _categoryWriteRepo.AddAsync(category);
                }
            }
        }

        // Remove categories
        if (dto.DeleteCategoryIds != null)
        {
            foreach (var cid in dto.DeleteCategoryIds)
            {
                if (!Guid.TryParse(cid, out var guid)) continue;

                var link = entity.ProductCategories
                    .FirstOrDefault(pc => pc.CategoryId == guid && !pc.IsDeleted);

                if (link != null)
                {
                    link.IsDeleted = true;
                    link.DeletedDate = DateTime.UtcNow;
                }
            }
        }


        // =====================================================
        // 8) SAVE CHANGES
        // =====================================================
        try
        {
            await _writeRepo.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new GlobalAppException("CONCURRENCY_CONFLICT_PRODUCT_UPDATED_OR_DELETED");
        }
    }

    // ================================================
    //                DELETE
    // ================================================
    public async Task DeleteAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(x => x.Id == gid)
                     ?? throw new GlobalAppException("Product not found!");

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }

    // ================================================
    //           STATUS BOOLEAN METHODS
    // ================================================
    public async Task SetIsActiveAsync(string id, bool value)
    {
        var gid = ParseGuidOrThrow(id);

        var product = await _readRepo.GetAsync(x => x.Id == gid && !x.IsDeleted, enableTracking: true)
                      ?? throw new GlobalAppException("Product not found!");

        var wasInactive = product.IsActive == false;
        var wasNeverPublished = product.PublishedAt == null;

        if (value && wasNeverPublished)
            product.PublishedAt = DateTime.UtcNow;

        product.IsActive = value;
        product.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(product);
        await _writeRepo.CommitAsync();

        // ✅ Aktivləşəndə 1 dəfə trigger et
        if (value && wasInactive)
            await _campaignService.CreateAndScheduleNewProductCampaignAsync(product);
    }
    public async Task SetIsFeaturedAsync(string id, bool value)
        => await UpdateBooleanField(id, p => p.IsFeatured = value);

    public async Task SetIsBestSellerAsync(string id, bool value)
        => await UpdateBooleanField(id, p => p.IsBestseller = value);

    public async Task SetIsNewAsync(string id, bool value)
        => await UpdateBooleanField(id, p => p.IsNew = value);

    public async Task SetIsOnSaleAsync(string id, bool value)
        => await UpdateBooleanField(id, p => p.IsOnSale = value);

    private async Task UpdateBooleanField(string id, Action<Product> update)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(x => x.Id == gid && !x.IsDeleted)
                     ?? throw new GlobalAppException("Product not found!");

        update(entity);
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }

    public async Task<PagedResult<ProductDto>> GetAllBestSellerAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        // Total count
        var total = await _readRepo.GetCountAsync(p => !p.IsDeleted && p.IsBestseller == true && p.IsActive == true
        );

        // Main paged query
        var products = await _readRepo.GetPagedAsync(
            predicate: p => !p.IsDeleted && p.IsBestseller == true && p.IsActive == true
                            && p.ProductCategories.Any(pc => !pc.IsDeleted),
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages) // filter YOX
                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)
                .Include(p => p.Reviews),
            orderBy: q => q.OrderByDescending(p => p.CreatedDate),
            page: page,
            pageSize: pageSize,
            enableTracking: false
        );

        var dtoList = _mapper.Map<List<ProductDto>>(products);

        return new PagedResult<ProductDto>
        {
            Items = dtoList,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<ProductDto>> GetAllOnSaleAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        // Total count
        var total = await _readRepo.GetCountAsync(p => !p.IsDeleted && p.IsOnSale == true && p.IsActive == true
        );

        // Main paged query
        var products = await _readRepo.GetPagedAsync(
            predicate: p => !p.IsDeleted && p.IsOnSale == true && p.IsActive == true
                            && p.ProductCategories.Any(pc => !pc.IsDeleted),
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages) // filter YOX
                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)
                .Include(p => p.Reviews)
            ,
            orderBy: q => q.OrderByDescending(p => p.CreatedDate),
            page: page,
            pageSize: pageSize,
            enableTracking: false
        );

        var dtoList = _mapper.Map<List<ProductDto>>(products);

        return new PagedResult<ProductDto>
        {
            Items = dtoList,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<ProductDto>> GetAllNewProductAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        // Total count
        var total = await _readRepo.GetCountAsync(p => !p.IsDeleted && p.IsNew == true && p.IsActive == true
        );

        // Main paged query
        var products = await _readRepo.GetPagedAsync(
            predicate: p => !p.IsDeleted && p.IsNew == true && p.IsActive == true
                            && p.ProductCategories.Any(pc => !pc.IsDeleted),
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages) // filter YOX
                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)
                .Include(p => p.Reviews),
            orderBy: q => q.OrderByDescending(p => p.CreatedDate),
            page: page,
            pageSize: pageSize,
            enableTracking: false
        );

        var dtoList = _mapper.Map<List<ProductDto>>(products);

        return new PagedResult<ProductDto>
        {
            Items = dtoList,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<ProductDto>> GetByCategoryAsync(string categoryId, int page, int pageSize)
    {
        if (!Guid.TryParse(categoryId, out var cid))
            throw new GlobalAppException("INVALID_CATEGORY_ID");

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        // Total count
        var total = await _readRepo.GetCountAsync(p => !p.IsDeleted && p.IsActive == true &&
                                                       p.ProductCategories.Any(pc =>
                                                           !pc.IsDeleted && pc.CategoryId == cid)
        );

        // Main paged query
        var products = await _readRepo.GetPagedAsync(
            predicate: p => !p.IsDeleted &&
                            p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid && p.IsActive == true),
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages) // filter YOX
                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)
                .Include(p => p.Reviews),
            orderBy: q => q.OrderByDescending(p => p.CreatedDate),
            page: page,
            pageSize: pageSize,
            enableTracking: false
        );

        var dtoList = _mapper.Map<List<ProductDto>>(products);

        return new PagedResult<ProductDto>
        {
            Items = dtoList,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    // ================================================
    //                  HELPERS
    // ================================================
    private static Guid ParseGuidOrThrow(string id)
    {
        if (!Guid.TryParse(id, out var gid))
            throw new GlobalAppException("Id format yanlışdır!");
        return gid;
    }

    private DateTime ParseDate(string str)
    {
        if (DateTime.TryParseExact(str, "dd:MM:yyyy", null, System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        throw new GlobalAppException("PublishedAt formatı dd:MM:yyyy olmalıdır!");
    }

    private const int ThumbW = 308;
    private const int ThumbH = 405;

    private async Task<string> CreateAndUploadThumbnailAsync(IFormFile original, string folder)
    {
        await using var input = original.OpenReadStream();
        using var image = await Image.LoadAsync(input);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(ThumbW, ThumbH),
            Mode = ResizeMode.Crop // dəqiq 308x405 olsun deyə
        }));

        await using var ms = new MemoryStream();
        await image.SaveAsync(ms, new JpegEncoder() { Quality = 85 });
        ms.Position = 0;

        // MemoryStream -> IFormFile
        var fileName = Path.GetFileNameWithoutExtension(original.FileName);
        var thumbName = $"{fileName}-thumb-{ThumbW}x{ThumbH}.jpg";

        IFormFile thumbFormFile = new FormFile(ms, 0, ms.Length, "file", thumbName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        return await _fileService.UploadFile(thumbFormFile, folder);
    }


// Verilənlərdən ilk dolu olanı qaytarır
    private static string? FirstNonEmpty(params string?[] vals)
        => vals.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

// SKU auto-generate (ad əsaslı) + unikal suffix
    private async Task<string> GenerateUniqueSkuAsync(string baseName)
    {
        // Məs: T-shirt Black Large -> TSHIRT-BLACK-LARGE (qısaldılmış)
        var baseSku = SanitizeSkuPart(baseName);
        if (string.IsNullOrWhiteSpace(baseSku))
            baseSku = $"PRD-{Guid.NewGuid().ToString("N")[..6]}";

        var candidate = baseSku;
        int i = 2;
        while (true)
        {
            var exists = await _readRepo.GetAsync(x => !x.IsDeleted && x.Sku == candidate);
            if (exists == null) return candidate;
            candidate = $"{baseSku}-{i}";
            i++;
        }
    }

// SLUG auto-generate (ad əsaslı) + unikal suffix
    private async Task<string> GenerateUniqueSlugAsync(string baseName)
    {
        var baseSlug = Slugify(baseName);
        if (string.IsNullOrWhiteSpace(baseSlug))
            baseSlug = $"product-{Guid.NewGuid().ToString("N")[..6]}";

        var candidate = baseSlug;
        int i = 2;
        while (true)
        {
            var exists = await _readRepo.GetAsync(x => !x.IsDeleted && x.Slug == candidate);
            if (exists == null) return candidate;
            candidate = $"{baseSlug}-{i}";
            i++;
        }
    }

// SKU üçün təmizləmə (latin, rəqəm, '-'; uzunluğu limitlə)
    private static string SanitizeSkuPart(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        input = RemoveDiacritics(input);

        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToUpperInvariant(ch));
            else if (ch == '-' || ch == '_' || char.IsWhiteSpace(ch)) sb.Append('-');
        }

        var res = sb.ToString().Trim('-');

        // çox uzundursa qısalt
        if (res.Length > 30) res = res[..30];

        // ardıcıl çox tire -> tək tire
        while (res.Contains("--")) res = res.Replace("--", "-");
        return res;
    }

// Slugify (latin kiçik hərf, '-'; ardıcıl tireləri birləşdir)
    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        input = RemoveDiacritics(input).ToLowerInvariant();

        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch == ' ' || ch == '-' || ch == '_' || ch == '/') sb.Append('-');
        }

        var slug = sb.ToString().Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        if (slug.Length > 80) slug = slug[..80];
        return slug;
    }

// Diakritika təmizləmə (ə,ö,ü,ğ,ç → aeoqc… kimi)
    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private async Task EnsureSkuSlugUniqueAsync(string sku, string slug, Guid? currentId)
    {
        // SKU
        var skuExists = await _readRepo.GetAsync(x =>
            !x.IsDeleted && x.Sku == sku && (currentId == null || x.Id != currentId.Value)
        );
        if (skuExists != null)
            throw new GlobalAppException("SKU_ALREADY_EXISTS");

        // Slug
        var slugExists = await _readRepo.GetAsync(x =>
            !x.IsDeleted && x.Slug == slug && (currentId == null || x.Id != currentId.Value)
        );
        if (slugExists != null)
            throw new GlobalAppException("SLUG_ALREADY_EXISTS");
    }
}