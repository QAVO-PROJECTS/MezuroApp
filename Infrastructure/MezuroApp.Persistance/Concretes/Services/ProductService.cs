using System.Globalization;
using System.Text;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Repositories.ProductImages;
using MezuroApp.Application.Abstracts.Repositories.WishlistItems;
using MezuroApp.Application.Abstracts.Repositories.Wishlists;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.Product.ProductFilter;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using MezuroApp.Persistance.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

public class ProductService : IProductService
{
    private readonly IProductReadRepository _readRepo;
    private readonly IProductWriteRepository _writeRepo;
    private readonly IProductImageWriteRepository _imageWriteRepo;
    private readonly IProductCategoryWriteRepository _categoryWriteRepo;
    private readonly IProductCategoryReadRepository _categoryReadRepo;
    private readonly IFileService _fileService;
    private readonly IWishlistItemReadRepository _wishlistReadRepo;
    private readonly IMapper _mapper;
    private readonly IEmailCampaignService _campaignService;
    private readonly ILogger<ProductService> _logger;
    private readonly IAuditHelper _audit;
    

    private const string ProductFolder = "products";

    public ProductService(
        IProductReadRepository readRepo,
        IProductWriteRepository writeRepo,
        IProductImageWriteRepository imageWriteRepo,
        IProductCategoryWriteRepository categoryWriteRepo,
        IProductCategoryReadRepository categoryReadRepo,
        IFileService fileService,
        IWishlistItemReadRepository wishlistReadRepo,
        IEmailCampaignService campaignService,
        IMapper mapper,
        ILogger<ProductService> logger,
        IAuditHelper audit)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _imageWriteRepo = imageWriteRepo;
        _categoryWriteRepo = categoryWriteRepo;
        _categoryReadRepo = categoryReadRepo;
        _fileService = fileService;
        _wishlistReadRepo = wishlistReadRepo;
        _mapper = mapper;
        _campaignService = campaignService;
        _logger = logger;
        _audit = audit;
    }

    // ================================================
    //                GET METHODS
    // ================================================
public async Task<PagedResult<ProductDto>> AdminSearchAsync(
    string term,
    string lang = "az",
    int page = 1,
    int pageSize = 20)
{
    if (page <= 0) page = 1;
    if (pageSize <= 0) pageSize = 20;

    term = (term ?? "").Trim();
    if (term.Length < 2)
        return new PagedResult<ProductDto>
        {
            Items = new List<ProductDto>(),
            Page = page,
            PageSize = pageSize,
            TotalCount = 0
        };

    var like = $"%{term}%";

    IQueryable<Product> q = _readRepo.Query()
        .AsNoTracking()
        .Where(p => !p.IsDeleted);

    q = q.Where(p =>
        // Product name match (unaccented)
        (lang == "en"
            ? EF.Functions.ILike(MezuroAppDbContext.Unaccent(p.NameEn), MezuroAppDbContext.Unaccent(like))
            : lang == "ru"
                ? EF.Functions.ILike(MezuroAppDbContext.Unaccent(p.NameRu), MezuroAppDbContext.Unaccent(like))
                : lang == "tr"
                    ? EF.Functions.ILike(MezuroAppDbContext.Unaccent(p.NameTr), MezuroAppDbContext.Unaccent(like))
                    : EF.Functions.ILike(MezuroAppDbContext.Unaccent(p.NameAz), MezuroAppDbContext.Unaccent(like))
        )
        ||
        // Category name match (unaccented) -> products under that category
        p.ProductCategories.Any(pc => !pc.IsDeleted &&
            (lang == "en"
                ? EF.Functions.ILike(MezuroAppDbContext.Unaccent(pc.Category.NameEn), MezuroAppDbContext.Unaccent(like))
                : lang == "ru"
                    ? EF.Functions.ILike(MezuroAppDbContext.Unaccent(pc.Category.NameRu), MezuroAppDbContext.Unaccent(like))
                    : lang == "tr"
                        ? EF.Functions.ILike(MezuroAppDbContext.Unaccent(pc.Category.NameTr), MezuroAppDbContext.Unaccent(like))
                        : EF.Functions.ILike(MezuroAppDbContext.Unaccent(pc.Category.NameAz), MezuroAppDbContext.Unaccent(like))
            )
        )
    );

    var total = await q.CountAsync();

    var products = await q
        .Include(p => p.Images.Where(i => !i.IsDeleted))
        .Include(p => p.ProductCategories.Where(pc => !pc.IsDeleted)).ThenInclude(pc => pc.Category)
        .OrderByDescending(p => p.CreatedDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var dto = _mapper.Map<List<ProductDto>>(products);

    return new PagedResult<ProductDto>
    {
        Items = dto,
        Page = page,
        PageSize = pageSize,
        TotalCount = total
    };
}
    public async Task<PagedResult<ProductDto>> AdminSortedAsync(AdminProductSortRequestDto r)
{
    if (r.Page <= 0) r.Page = 1;
    if (r.PageSize <= 0) r.PageSize = 20;

    Guid? cid = null;
    if (!string.IsNullOrWhiteSpace(r.CategoryId) && Guid.TryParse(r.CategoryId, out var parsed))
        cid = parsed;

    var q = _readRepo.Query()
        .AsNoTracking()
        .Where(p => !p.IsDeleted);

    if (cid.HasValue)
        q = q.Where(p => p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid.Value));

    var total = await q.CountAsync();

    // language-aware name selector (EF translatable)
    var nameKey = q.Select(p => new
    {
        Product = p,
        Name =
            r.Lang == "en" ? p.NameEn :
            r.Lang == "ru" ? p.NameRu :
            r.Lang == "tr" ? p.NameTr :
            p.NameAz
    });

    // apply sorting
    IQueryable<Product> sorted = r.Sort switch
    {
        AdminProductSort.NameAsc  => nameKey.OrderBy(x => x.Name).ThenByDescending(x => x.Product.CreatedDate).Select(x => x.Product),
        AdminProductSort.NameDesc => nameKey.OrderByDescending(x => x.Name).ThenByDescending(x => x.Product.CreatedDate).Select(x => x.Product),

        AdminProductSort.DateAsc  => q.OrderBy(p => p.CreatedDate),
        _                         => q.OrderByDescending(p => p.CreatedDate) // DateDesc default
    };

    var products = await sorted
        .Include(p => p.Images.Where(i => !i.IsDeleted))
        .Include(p => p.ProductCategories.Where(pc => !pc.IsDeleted))
            .ThenInclude(pc => pc.Category)
        .Include(p => p.Options.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.Option)
        .Include(p => p.Options.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
        .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
        .Skip((r.Page - 1) * r.PageSize)
        .Take(r.PageSize)
        .ToListAsync();

    var dto = _mapper.Map<List<ProductDto>>(products);

    foreach (var p in dto)
        if (p.Images != null)
            p.Images = p.Images.OrderBy(i => i.SortOrder ?? int.MaxValue).ToList();

    return new PagedResult<ProductDto>
    {
        Items = dto,
        Page = r.Page,
        PageSize = r.PageSize,
        TotalCount = total
    };
}
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
    public async Task<ProductDto> GetBySlugAsync(string slug)
    {
  

        var entity = await _readRepo.GetAsync(
            x => x.Slug == slug && !x.IsDeleted && x.IsActive == true,
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
        var wishlistCount = await _wishlistReadRepo.GetCountAsync(x => x.ProductId.ToString() == entity.Id.ToString());


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
            x => !x.IsDeleted ,
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
    public async Task<PagedResult<ProductDto>> FilterAsync(ProductFilterRequestDto r)
{
    if (r.Page <= 0) r.Page = 1;
    if (r.PageSize <= 0) r.PageSize = 20;

    IQueryable<Product> q = _readRepo.Query()
        .AsNoTracking()
        .Where(p => !p.IsDeleted && p.IsActive == true);

    // Category
    if (!string.IsNullOrWhiteSpace(r.CategoryId) && Guid.TryParse(r.CategoryId, out var cid))
    {
        q = q.Where(p => p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid));
    }

    // Price
    if (r.MinPrice.HasValue) q = q.Where(p => p.Price >= r.MinPrice.Value);
    if (r.MaxPrice.HasValue) q = q.Where(p => p.Price <= r.MaxPrice.Value);

    // Colors (ProductColor.Id)
    if (r.ColorIds?.Count > 0)
    {
        var colorGuids = r.ColorIds
            .Where(x => Guid.TryParse(x, out _))
            .Select(x => Guid.Parse(x))
            .ToList();
        if (colorGuids.Count > 0)
            q = q.Where(p => p.ProductColors.Any(pc => !pc.IsDeleted && colorGuids.Contains(pc.Id)));
    }

    // Option Values (ProductOptionValue.Id)
    // məhsulun option values-unda o dəyər varsa filterlə
    if (r.OptionValueIds?.Count > 0)
    {
        var valueGuids = r.OptionValueIds
            .Where(x => Guid.TryParse(x, out _))
            .Select(x => Guid.Parse(x))
            .ToList();
        if (valueGuids.Count > 0)
            q = q.Where(p =>
                p.Options.Any(po => !po.IsDeleted &&
                    po.Values.Any(v => !v.IsDeleted && valueGuids.Contains(v.Id))));
    }

    // Sort
    q = (r.Sort ?? "newest").ToLowerInvariant() switch
    {
        "price_asc" => q.OrderBy(p => p.Price),
        "price_desc" => q.OrderByDescending(p => p.Price),
        "rating_desc" => q.OrderByDescending(p => p.RatingAverage),
        "rating_asc" => q.OrderBy(p => p.RatingAverage),
        _ => q.OrderByDescending(p => p.CreatedDate) // newest
    };

    var total = await q.CountAsync();

    // list query (include lazım olanlar)
    var items = await q
        .Skip((r.Page - 1) * r.PageSize)
        .Take(r.PageSize)
        .Include(p => p.Images.Where(i => !i.IsDeleted))
        .Include(p => p.ProductCategories.Where(pc => !pc.IsDeleted))
            .ThenInclude(pc => pc.Category)
        .Include(p => p.Options.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.Option)
        .Include(p => p.Options.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
        .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
        .ToListAsync();

    var dtoList = _mapper.Map<List<ProductDto>>(items);

    foreach (var p in dtoList)
    {
        if (p.Images != null)
            p.Images = p.Images.OrderBy(i => i.SortOrder ?? int.MaxValue).ToList();
    }

    return new PagedResult<ProductDto>
    {
        Items = dtoList,
        Page = r.Page,
        PageSize = r.PageSize,
        TotalCount = total
    };
}
    public async Task<ProductFilterMetaDto> GetFilterMetaAsync(
    string? categoryId = null,
    string lang = "az")
{
    IQueryable<Product> q = _readRepo.Query()
        .AsNoTracking()
        .Where(p => !p.IsDeleted && p.IsActive == true);
    Guid? cid = null;
    if (!string.IsNullOrWhiteSpace(categoryId) && Guid.TryParse(categoryId, out var parsed))
        cid = parsed;

    if (cid.HasValue)
    {
        q = q.Where(p => p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid.Value));
    }

    // PRICE RANGE (optional, panel üçün yaxşı olur)
    var min = await q.MinAsync(p => (decimal?)p.Price) ?? 0m;
    var max = await q.MaxAsync(p => (decimal?)p.Price) ?? 0m;

    // COLORS + COUNT (ProductColor-dan)
    var colors = await _readRepo.Query()
        .Where(p => !p.IsDeleted && p.IsActive == true)
        .Where(p => p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid))
        .SelectMany(p => p.ProductColors.Where(c => !c.IsDeleted))
        .GroupBy(c => new
        {
            c.Id,
            c.ColorCode,
            Name =
                lang == "az" ? c.ColorNameAz :
                lang == "en" ? c.ColorNameEn :
                lang == "ru" ? c.ColorNameRu :
                lang == "tr" ? c.ColorNameTr :
                c.ColorNameAz
        })
        .Select(g => new ColorFilterItemDto
        {
            Id = g.Key.Id.ToString(),
            Name = g.Key.Name ?? "",
            Code = g.Key.ColorCode ?? "",
            Count = g.Count()
        })
        .ToListAsync();

    // OPTIONS -> VALUES + COUNT
    // Option adı: ProductOption.CustomName varsa onu seç, yoxsa Option.Name*
    var optionRows = await q
        .SelectMany(p => p.Options.Where(po => !po.IsDeleted),
            (p, po) => new
            {
                ProductId = p.Id,
                OptionId = po.OptionId,
                OptionName =
                    PickLang(po.CustomNameAz ?? po.Option.NameAz,
                             po.CustomNameEn ?? po.Option.NameEn,
                             po.CustomNameRu ?? po.Option.NameRu,
                             po.CustomNameTr ?? po.Option.NameTr,
                             lang)
            })
        .ToListAsync();

    var valueRows = await q
        .SelectMany(p => p.Options.Where(po => !po.IsDeleted),
            (p, po) => new { p.Id, po })
        .SelectMany(x => x.po.Values.Where(v => !v.IsDeleted),
            (x, v) => new
            {
                ProductId = x.Id,
                OptionId = x.po.OptionId,
                ValueId = v.Id,
                ValueName = PickLang(v.ValueAz, v.ValueEn, v.ValueRu, v.ValueTr, lang)
            })
        .ToListAsync();

    var options = optionRows
        .GroupBy(x => new { x.OptionId, x.OptionName })
        .Select(g => new OptionFilterItemDto
        {
            Id = g.Key.OptionId.ToString(),
            Name = g.Key.OptionName,
            Values = valueRows
                .Where(v => v.OptionId == g.Key.OptionId)
                .GroupBy(v => new { v.ValueId, v.ValueName })
                .Select(vg => new OptionValueFilterItemDto
                {
                    Id = vg.Key.ValueId.ToString(),
                    Name = vg.Key.ValueName,
                    Count = vg.Select(z => z.ProductId).Distinct().Count()
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .ToList()
        })
        .OrderBy(x => x.Name)
        .ToList();

    return new ProductFilterMetaDto
    {
        Colors = colors,
        Options = options,
        PriceRange = new PriceRangeMetaDto { Min = min, Max = max }
    };
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
    public async Task<PagedResult<ProductDto>> AdminFilterByPriceAsync(AdminProductFilterRequestDto r)
    {
        if (r.Page <= 0) r.Page = 1;
        if (r.PageSize <= 0) r.PageSize = 20;

        Guid? cid = null;
        if (!string.IsNullOrWhiteSpace(r.CategoryId) && Guid.TryParse(r.CategoryId, out var parsed))
            cid = parsed;

        var q = _readRepo.Query()
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        // category filter (optional)
        if (cid.HasValue)
            q = q.Where(p => p.ProductCategories.Any(pc => !pc.IsDeleted && pc.CategoryId == cid.Value));

        // price filters (optional)
        if (r.MinPrice.HasValue)
            q = q.Where(p => p.Price >= r.MinPrice.Value);

        if (r.MaxPrice.HasValue)
            q = q.Where(p => p.Price <= r.MaxPrice.Value);

        var total = await q.CountAsync();

        var products = await q
            .Include(p => p.Images.Where(i => !i.IsDeleted))
            .Include(p => p.ProductCategories.Where(pc => !pc.IsDeleted))
            .ThenInclude(pc => pc.Category)
            .Include(p => p.Options.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.Option)
            .Include(p => p.Options.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
            .Include(p => p.ProductColors.Where(pc => !pc.IsDeleted))
            .OrderByDescending(p => p.CreatedDate)
            .Skip((r.Page - 1) * r.PageSize)
            .Take(r.PageSize)
            .ToListAsync();

        var dto = _mapper.Map<List<ProductDto>>(products);

        // şəkilləri sort et
        foreach (var p in dto)
            if (p.Images != null)
                p.Images = p.Images.OrderBy(i => i.SortOrder ?? int.MaxValue).ToList();

        return new PagedResult<ProductDto>
        {
            Items = dto,
            Page = r.Page,
            PageSize = r.PageSize,
            TotalCount = total
        };
    }
    public async Task<PagedResult<ProductDto>> GetAllProductForAdminAsync(int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        // Total count
        var total = await _readRepo.GetCountAsync(p => !p.IsDeleted  
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
    public async Task<PagedResult<ProductDto>> GetAllStatusFilteredProductForAdminAsync(int page, int pageSize,bool status)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        // Total count
        var total = await _readRepo.GetCountAsync(p => !p.IsDeleted  
        );

        // Main paged query
        var products = await _readRepo.GetPagedAsync(
            predicate: p => !p.IsDeleted && p.IsNew == true && p.IsActive == status
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
    //                CREATE
    // ================================================
    public async Task<string> CreateAsync(CreateProductDto dto)
    {
        // 1) Map Product basic fields
        var entity = _mapper.Map<Product>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedDate = DateTime.UtcNow;
        entity.LastUpdatedDate = entity.CreatedDate;
        entity.IsDeleted = false;

        var baseName = FirstNonEmpty(dto.NameEn, dto.NameAz, dto.NameTr, dto.NameRu)
                       ?? $"PRD-{entity.Id.ToString("N")[..6]}";

        entity.Sku = await GenerateUniqueSkuAsync(baseName);
        entity.Slug = await GenerateUniqueSlugAsync(baseName);

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
                await _audit.LogAsync(
            entityType: "Products",
            action: "CREATE",
            eventName: "PRODUCT_CREATED",
            entityId: entity.Id,
            oldValues: null,
            newValues: new Dictionary<string, object>
            {
                ["NameAz"] = entity.NameAz ?? "",
                ["NameEn"] = entity.NameEn ?? "",
                ["NameRu"] = entity.NameRu ?? "",
                ["NameTr"] = entity.NameTr ?? "",
                ["DescriptionAz"] = entity.DescriptionAz ?? "",
                ["DescriptionEn"] = entity.DescriptionEn ?? "",
                ["DescriptionRu"] = entity.DescriptionRu ?? "",
                ["DescriptionTr"] = entity.DescriptionTr ?? "",
                ["ShortDescriptionAz"] = entity.ShortDescriptionAz ?? "",
                ["ShortDescriptionEn"] = entity.ShortDescriptionEn ?? "",
                ["ShortDescriptionRu"] = entity.ShortDescriptionRu ?? "",
                ["ShortDescriptionTr"] = entity.ShortDescriptionTr ?? "",
                ["IsActive"] = entity.IsActive ?? false,
                ["IsNew"] = entity.IsNew ?? false,
                ["IsBestseller"] = entity.IsBestseller ?? false,
                ["IsOnSale"] = entity.IsOnSale ?? false,
                ["ImageFiles"] = entity.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                ["CompareAtPrice"] = entity.CompareAtPrice,
                ["StockQuantity"] = entity.StockQuantity,
                ["ProductCategoryIds"] = entity.ProductCategories?.Select(pc => pc.CategoryId.ToString()).ToList() ?? new List<string>(),
                ["MetaTitleAz"] = entity.MetaTitleAz ?? "",
                ["MetaTitleEn"] = entity.MetaTitleEn ?? "",
                ["MetaTitleRu"] = entity.MetaTitleRu ?? "",
                ["MetaTitleTr"] = entity.MetaTitleTr ?? "",
                ["MetaDescriptionAz"] = entity.MetaDescriptionAz ?? "",
                ["MetaDescriptionEn"] = entity.MetaDescriptionEn ?? "",
                ["MetaDescriptionRu"] = entity.MetaDescriptionRu ?? "",
                ["MetaDescriptionTr"] = entity.MetaDescriptionTr ?? "",
                ["Price"] = entity.Price,
                ["Sku"] = entity.Sku 
            }
        );

        return entity.Id.ToString();
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
        var oldValues = new Dictionary<string, object>
            {
                ["NameAz"] = entity.NameAz ?? "",
                ["NameEn"] = entity.NameEn ?? "",
                ["NameRu"] = entity.NameRu ?? "",
                ["NameTr"] = entity.NameTr ?? "",
                ["DescriptionAz"] = entity.DescriptionAz ?? "",
                ["DescriptionEn"] = entity.DescriptionEn ?? "",
                ["DescriptionRu"] = entity.DescriptionRu ?? "",
                ["DescriptionTr"] = entity.DescriptionTr ?? "",
                ["ShortDescriptionAz"] = entity.ShortDescriptionAz ?? "",
                ["ShortDescriptionEn"] = entity.ShortDescriptionEn ?? "",
                ["ShortDescriptionRu"] = entity.ShortDescriptionRu ?? "",
                ["ShortDescriptionTr"] = entity.ShortDescriptionTr ?? "",
                ["IsActive"] = entity.IsActive ?? false,
                ["IsNew"] = entity.IsNew ?? false,
                ["IsBestseller"] = entity.IsBestseller ?? false,
                ["IsOnSale"] = entity.IsOnSale ?? false,
                ["ImageFiles"] = entity.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                ["CompareAtPrice"] = entity.CompareAtPrice,
                ["StockQuantity"] = entity.StockQuantity,
                ["ProductCategoryIds"] = entity.ProductCategories?.Select(pc => pc.CategoryId.ToString()).ToList() ??
                                         new List<string>(),
                ["MetaTitleAz"] = entity.MetaTitleAz ?? "",
                ["MetaTitleEn"] = entity.MetaTitleEn ?? "",
                ["MetaTitleRu"] = entity.MetaTitleRu ?? "",
                ["MetaTitleTr"] = entity.MetaTitleTr ?? "",
                ["MetaDescriptionAz"] = entity.MetaDescriptionAz ?? "",
                ["MetaDescriptionEn"] = entity.MetaDescriptionEn ?? "",
                ["MetaDescriptionRu"] = entity.MetaDescriptionRu ?? "",
                ["MetaDescriptionTr"] = entity.MetaDescriptionTr ?? "",
                ["Price"] = entity.Price,
                ["Sku"] = entity.Sku
            }
            ;


        // ======================================
        // 2) SKU və Slug unikallığı
        // ======================================


        // ======================================
        // 3) Property-lərin update olunması
        // ======================================
        var currentImages = entity.Images; // referansı saxla
        if (dto.Price == null)
        {
            dto.Price = entity.Price;
        }

        if (dto.CompareAtPrice == null)
        {
            dto.CompareAtPrice = entity.CompareAtPrice;
        }
        _mapper.Map(dto, entity);
        entity.Images = currentImages ?? new List<ProductImage>();
        var baseName = FirstNonEmpty(entity.NameEn, entity.NameAz, entity.NameTr, entity.NameRu)
                       ?? $"PRD-{entity.Id.ToString("N")[..6]}";


        if (string.IsNullOrWhiteSpace(entity.Sku))
            entity.Sku = await GenerateUniqueSkuAsync(baseName);
        else
            entity.Sku = entity.Sku.Trim();

        if (string.IsNullOrWhiteSpace(entity.Slug))
            entity.Slug = await GenerateUniqueSlugAsync(baseName);
        else
            entity.Slug = Slugify(entity.Slug.Trim());


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
       

        _logger.LogInformation("NewImageFiles count: {c}", dto.NewImageFiles?.Count ?? 0);
        _logger.LogInformation("NewImages meta count: {c}", dto.NewImages?.Count ?? 0);
        var imageFiles = dto.NewImageFiles ?? new List<IFormFile>();
        var metaList = dto.NewImages ?? new List<UpdateProductImageMetaDto>();

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

             
                
                var productImage = new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ProductId = entity.Id,
                    ImageUrl = imgUrl,
                    ThumbnailUrl = thumbUrl,
                    AltText = meta.AltText ?? "",
                    IsPrimary = meta.IsPrimary,
                    SortOrder = meta.SortOrder ?? entity.Images.Count,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _imageWriteRepo.AddAsync(productImage);
                await _imageWriteRepo.CommitAsync();
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
       
            await _writeRepo.CommitAsync();
            await _audit.LogAsync(
                "Products",
                "UPDATE",
                "PRODUCT_UPDATED",
                entity.Id,
                oldValues,
                new Dictionary<string, object>
            {
                ["NameAz"] = entity.NameAz ?? "",
                ["NameEn"] = entity.NameEn ?? "",
                ["NameRu"] = entity.NameRu ?? "",
                ["NameTr"] = entity.NameTr ?? "",
                ["DescriptionAz"] = entity.DescriptionAz ?? "",
                ["DescriptionEn"] = entity.DescriptionEn ?? "",
                ["DescriptionRu"] = entity.DescriptionRu ?? "",
                ["DescriptionTr"] = entity.DescriptionTr ?? "",
                ["ShortDescriptionAz"] = entity.ShortDescriptionAz ?? "",
                ["ShortDescriptionEn"] = entity.ShortDescriptionEn ?? "",
                ["ShortDescriptionRu"] = entity.ShortDescriptionRu ?? "",
                ["ShortDescriptionTr"] = entity.ShortDescriptionTr ?? "",
                ["IsActive"] = entity.IsActive ?? false,
                ["IsNew"] = entity.IsNew ?? false,
                ["IsBestseller"] = entity.IsBestseller ?? false,
                ["IsOnSale"] = entity.IsOnSale ?? false,
                ["ImageFiles"] = entity.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                ["CompareAtPrice"] = entity.CompareAtPrice,
                ["StockQuantity"] = entity.StockQuantity,
                ["ProductCategoryIds"] = entity.ProductCategories?.Select(pc => pc.CategoryId.ToString()).ToList() ?? new List<string>(),
                ["MetaTitleAz"] = entity.MetaTitleAz ?? "",
                ["MetaTitleEn"] = entity.MetaTitleEn ?? "",
                ["MetaTitleRu"] = entity.MetaTitleRu ?? "",
                ["MetaTitleTr"] = entity.MetaTitleTr ?? "",
                ["MetaDescriptionAz"] = entity.MetaDescriptionAz ?? "",
                ["MetaDescriptionEn"] = entity.MetaDescriptionEn ?? "",
                ["MetaDescriptionRu"] = entity.MetaDescriptionRu ?? "",
                ["MetaDescriptionTr"] = entity.MetaDescriptionTr ?? "",
                ["Price"] = entity.Price,
                ["Sku"] = entity.Sku 
            }
        );
        
    
    }

    // ================================================
    //                DELETE
    // ================================================
    public async Task DeleteAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(x => x.Id == gid)
                     ?? throw new GlobalAppException("Product not found!");
        var oldValues = new Dictionary<string, object>
            {
                ["NameAz"] = entity.NameAz ?? "",
                ["NameEn"] = entity.NameEn ?? "",
                ["NameRu"] = entity.NameRu ?? "",
                ["NameTr"] = entity.NameTr ?? "",
                ["DescriptionAz"] = entity.DescriptionAz ?? "",
                ["DescriptionEn"] = entity.DescriptionEn ?? "",
                ["DescriptionRu"] = entity.DescriptionRu ?? "",
                ["DescriptionTr"] = entity.DescriptionTr ?? "",
                ["ShortDescriptionAz"] = entity.ShortDescriptionAz ?? "",
                ["ShortDescriptionEn"] = entity.ShortDescriptionEn ?? "",
                ["ShortDescriptionRu"] = entity.ShortDescriptionRu ?? "",
                ["ShortDescriptionTr"] = entity.ShortDescriptionTr ?? "",
                ["IsActive"] = entity.IsActive ?? false,
                ["IsNew"] = entity.IsNew ?? false,
                ["IsBestseller"] = entity.IsBestseller ?? false,
                ["IsOnSale"] = entity.IsOnSale ?? false,
                ["ImageFiles"] = entity.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                ["CompareAtPrice"] = entity.CompareAtPrice,
                ["StockQuantity"] = entity.StockQuantity,
                ["ProductCategoryIds"] = entity.ProductCategories?.Select(pc => pc.CategoryId.ToString()).ToList() ??
                                         new List<string>(),
                ["MetaTitleAz"] = entity.MetaTitleAz ?? "",
                ["MetaTitleEn"] = entity.MetaTitleEn ?? "",
                ["MetaTitleRu"] = entity.MetaTitleRu ?? "",
                ["MetaTitleTr"] = entity.MetaTitleTr ?? "",
                ["MetaDescriptionAz"] = entity.MetaDescriptionAz ?? "",
                ["MetaDescriptionEn"] = entity.MetaDescriptionEn ?? "",
                ["MetaDescriptionRu"] = entity.MetaDescriptionRu ?? "",
                ["MetaDescriptionTr"] = entity.MetaDescriptionTr ?? "",
                ["Price"] = entity.Price,
                ["Sku"] = entity.Sku
            }
            ;

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
        await _audit.LogAsync(
            "Products",
            "DELETE",
            "PRODUCT_DELETED",
            entity.Id,
            oldValues,
            null
        );
    }

    // ================================================
    //           STATUS BOOLEAN METHODS
    // ================================================
    public async Task SetIsActiveAsync(string id, bool value)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(x => x.Id == gid && !x.IsDeleted, enableTracking: true)
                      ?? throw new GlobalAppException("Product not found!");
        
        var oldValues = new Dictionary<string, object>
        {
            ["NameAz"] = entity.NameAz ?? "",
            ["NameEn"] = entity.NameEn ?? "",
            ["NameRu"] = entity.NameRu ?? "",
            ["NameTr"] = entity.NameTr ?? "",
     
            ["IsActive"] = entity.IsActive ?? false
        };

        var wasInactive = entity.IsActive == false;
        var wasNeverPublished = entity.PublishedAt == null;

        if (value && wasNeverPublished)
            entity.PublishedAt = DateTime.UtcNow;

        entity.IsActive = value;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();

        // ✅ Aktivləşəndə 1 dəfə trigger et
        if (value && wasInactive)
            await _campaignService.CreateAndScheduleNewProductCampaignAsync(entity);
        await _audit.LogAsync(
                "Products",
                "UPDATE",
                "PRODUCT_UPDATED",
                entity.Id,
                oldValues,
                new Dictionary<string, object>
                {
                    ["NameAz"] = entity.NameAz ?? "",
                    ["NameEn"] = entity.NameEn ?? "",
                    ["NameRu"] = entity.NameRu ?? "",
                    ["NameTr"] = entity.NameTr ?? "",
                    ["IsActive"] = entity.IsActive ?? false
                 
                 
                }
            );
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
    private static string PickLang(string? az, string? en, string? ru, string? tr, string lang)
    {
        lang = (lang ?? "az").ToLowerInvariant();
        return lang switch
        {
            "en" => en ?? az ?? ru ?? tr ?? "",
            "ru" => ru ?? az ?? en ?? tr ?? "",
            "tr" => tr ?? az ?? en ?? ru ?? "",
            _ => az ?? en ?? ru ?? tr ?? ""
        };
    }

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