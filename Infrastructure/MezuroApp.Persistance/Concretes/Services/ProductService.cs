using System.Globalization;
using System.Text;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class ProductService : IProductService
{
    private readonly IProductReadRepository _readRepo;
    private readonly IProductWriteRepository _writeRepo;
    private readonly IProductCategoryWriteRepository _categoryWriteRepo;
    private readonly IProductCategoryReadRepository _categoryReadRepo;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    private const string ProductFolder = "products";

    public ProductService(
        IProductReadRepository readRepo,
        IProductWriteRepository writeRepo,
        IProductCategoryWriteRepository categoryWriteRepo,
        IProductCategoryReadRepository categoryReadRepo,
        IFileService fileService,
        IMapper mapper)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _categoryWriteRepo = categoryWriteRepo;
        _categoryReadRepo = categoryReadRepo;
        _fileService = fileService;
        _mapper = mapper;
    }

    // ================================================
    //                GET METHODS
    // ================================================
    public async Task<ProductDto> GetByIdAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);
        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
              
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages)   // filter YOX

                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)
        );
        if (entity == null) throw new GlobalAppException("Product not found!");

        return _mapper.Map<ProductDto>(entity);
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var entities = await _readRepo.GetAllAsync(
            x => !x.IsDeleted,
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
           
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages)   // filter YOX

                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)

        );

        return _mapper.Map<List<ProductDto>>(entities);
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
        entity.Sku  = string.IsNullOrWhiteSpace(dto.Sku)  ? await GenerateUniqueSkuAsync(baseName)  : dto.Sku!.Trim();
        entity.Slug = string.IsNullOrWhiteSpace(dto.Slug) ? await GenerateUniqueSlugAsync(baseName) : Slugify(dto.Slug!.Trim());

        // 3) Unikal yoxla (əlavə təhlükəsizlik)
        await EnsureSkuSlugUniqueAsync(entity.Sku, entity.Slug, null);

        // Əgər PublishedAt gəlməyibsə və IsActive true-dursa → indi yazılır
        if (entity.IsActive == true && entity.PublishedAt == null)
            entity.PublishedAt = DateTime.UtcNow;

        // 2) SKU & Slug unikal yoxla
    

        // 3) Şəkil yükləmə (metadata-driven, mismatch-safe)
        var imageEntities = new List<ProductImage>();

        var imageFiles = dto.ImageFiles ?? new List<IFormFile>();
        var thumbFiles = dto.ThumbnailImageFiles ?? new List<IFormFile>();
        var metas = dto.Images ?? new List<CreateProductImageMetaDto>();

        // Eyni image/thumbnail faylının iki dəfə istifadə olunmaması üçün
        var usedImageIdx = new HashSet<int>();
        var usedThumbIdx = new HashSet<int>();

        // 3a. Əgər meta gəlirsə → meta əsasında upload et
        if (metas.Count > 0)
        {
            // FileIndex-ə görə sırala, sortOrder həm də bu sıraya görə gedəcək
            foreach (var meta in metas.OrderBy(m => m.FileIndex))
            {
                if (meta.FileIndex < 0 || meta.FileIndex >= imageFiles.Count)
                    continue; // səhv index → atla

                if (usedImageIdx.Contains(meta.FileIndex))
                    continue; // təkrarlanmış image → atla

                var file = imageFiles[meta.FileIndex];
                IFormFile? thumb = null;

                if (meta.ThumbnailIndex >= 0 && meta.ThumbnailIndex < thumbFiles.Count)
                {
                    if (!usedThumbIdx.Contains(meta.ThumbnailIndex))
                        thumb = thumbFiles[meta.ThumbnailIndex];
                }

                // Faylları yüklə
                var imageUrl = await _fileService.UploadFile(file, ProductFolder);
                var thumbUrl = thumb != null ? await _fileService.UploadFile(thumb, ProductFolder) : null;

                imageEntities.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = thumbUrl,
                    AltText = meta.AltText ?? "",           // DB NOT NULL riskini önləyir
                    IsPrimary = meta.IsPrimary,
                    SortOrder = imageEntities.Count
                });

                usedImageIdx.Add(meta.FileIndex);
                if (thumb != null) usedThumbIdx.Add(meta.ThumbnailIndex);
            }

            // Metada olmayan qalan image faylları da əlavə etmək istəsən:
            // (istəmirsənsə bu bloku silə bilərsən)
            for (int i = 0; i < imageFiles.Count; i++)
            {
                if (usedImageIdx.Contains(i)) continue;
                var file = imageFiles[i];
                var imageUrl = await _fileService.UploadFile(file, ProductFolder);

                imageEntities.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = null,                    // thumbnail yoxdur
                    AltText = "",                           // default
                    IsPrimary = false,
                    SortOrder = imageEntities.Count
                });
            }
        }
        else
        {
            // 3b. Meta yoxdursa → mövcud fayl sayına görə təhlükəsiz davran
            var imgCount = imageFiles.Count;
            var thumbCount = thumbFiles.Count;

            for (int i = 0; i < imgCount; i++)
            {
                var file = imageFiles[i];
                var thumb = (i < thumbCount) ? thumbFiles[i] : null;

                var imageUrl = await _fileService.UploadFile(file, ProductFolder);
                var thumbUrl = thumb != null ? await _fileService.UploadFile(thumb, ProductFolder) : null;

                imageEntities.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imageUrl,
                    ThumbnailUrl = thumbUrl,
                    AltText = "",                           // default
                    IsPrimary = (i == 0),                   // heç meta yoxdursa 1-cini primary et
                    SortOrder = i
                });
            }
        }

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
        enableTracking:true
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
    var thumbFiles = dto.NewThumbnailImageFiles ?? new List<IFormFile>();
    var metaList  = dto.NewImages ?? new List<UpdateProductImageMetaDto>();

    foreach (var meta in metaList)
    {
        bool isNew = string.IsNullOrWhiteSpace(meta.Id);

        // ===============================
        // 5.1) YENİ IMAGE YARAT (ID = null)
        // ===============================
        if (isNew)
        {
            if (meta.FileIndex < 0 || meta.FileIndex >= imageFiles.Count)
                continue;

            var imgFile = imageFiles[meta.FileIndex];
            var thumbFile = (meta.ThumbnailIndex >= 0 && meta.ThumbnailIndex < thumbFiles.Count)
                                ? thumbFiles[meta.ThumbnailIndex]
                                : null;

            var imgUrl = await _fileService.UploadFile(imgFile, ProductFolder);
            var thumbUrl = thumbFile != null
                            ? await _fileService.UploadFile(thumbFile, ProductFolder)
                            : null;

            
            entity.Images.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = entity.Id,
                ImageUrl = imgUrl,
                ThumbnailUrl = thumbUrl,
                AltText = meta.AltText ?? "",
                IsPrimary = meta.IsPrimary,
                SortOrder = entity.Images.Count
            });

            continue;
        }


        // ===============================
        // 5.2) MÖVCUD IMAGE UPDATE (ID != null)
        // ===============================
        var imgEntity = entity.Images.FirstOrDefault(
            x => x.Id.ToString() == meta.Id && !x.IsDeleted
        );

        if (imgEntity == null)
            continue;

        // Şəkli dəyişdir
        if (meta.FileIndex >= 0 && meta.FileIndex < imageFiles.Count)
        {
            var newImgFile = imageFiles[meta.FileIndex];
            imgEntity.ImageUrl = await _fileService.UploadFile(newImgFile, ProductFolder);
        }

        // Thumbnail dəyişdir
        if (meta.ThumbnailIndex >= 0 && meta.ThumbnailIndex < thumbFiles.Count)
        {
            var newThumbFile = thumbFiles[meta.ThumbnailIndex];
            imgEntity.ThumbnailUrl = await _fileService.UploadFile(newThumbFile, ProductFolder);
        }

        // Meta update
        imgEntity.AltText = meta.AltText ?? imgEntity.AltText;
        imgEntity.IsPrimary = meta.IsPrimary;
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
        var product = await _readRepo.GetAsync(x => x.Id == gid && !x.IsDeleted)
                      ?? throw new GlobalAppException("Product not found!");

        if (product.PublishedAt == null && value)
            product.PublishedAt = DateTime.UtcNow;

        product.IsActive = value;
        product.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(product);
        await _writeRepo.CommitAsync();
    }

    public async Task SetIsFeaturedAsync(string id, bool value)
        => await UpdateBooleanField(id, p => p.IsFeatured = value);

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
    
    public async Task<PagedResult<ProductDto>> GetByCategoryAsync(string categoryId, int page, int pageSize)
    {
        if (!Guid.TryParse(categoryId, out var cid))
            throw new GlobalAppException("INVALID_CATEGORY_ID");

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        // Total count
        var total = await _readRepo.GetCountAsync(
            p => !p.IsDeleted &&
                 p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid)
        );

        // Main paged query
        var products = await _readRepo.GetPagedAsync(
            predicate: p => !p.IsDeleted &&
                            p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid),
            q => q.Include(x => x.Images.Where(x => !x.IsDeleted))
                .Include(x => x.ProductCategories.Where(x => !x.IsDeleted))
                .ThenInclude(pc => pc.Category)
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Option) // <-- ƏSAS DÜZƏLİŞ
                .Include(p => p.Options.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
              
                .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                .ThenInclude(pc => pc.ColorImages)   // filter YOX

                .Include(p => p.ProductColors)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage),

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