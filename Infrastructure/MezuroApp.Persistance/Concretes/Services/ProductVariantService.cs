using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.OptionValues;
using MezuroApp.Application.Abstracts.Repositories.ProductColors;
using MezuroApp.Application.Abstracts.Repositories.ProductVariants;
using MezuroApp.Application.Abstracts.Repositories.ProductVariantOptionValues;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductOptions;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductVariant;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class ProductVariantService :IProductVariantService
{
    private readonly IProductVariantReadRepository _vr;
    private readonly IProductVariantWriteRepository _vw;
    private readonly IProductColorReadRepository _colorReadRepository;

    private readonly IProductVariantOptionValueReadRepository _vovr;
    private readonly IProductVariantOptionValueWriteRepository _vovw;

    private readonly IProductReadRepository _pr;
    private readonly IProductWriteRepository _pw;

    private readonly IProductOptionValueReadRepository _ovr;
    private readonly IMapper _mapper;
    private readonly IAuditHelper _audit;

    public ProductVariantService(
        IProductVariantReadRepository vr,
        IProductVariantWriteRepository vw,
        IProductColorReadRepository colorReadRepository,
        IProductVariantOptionValueReadRepository vovr,
        IProductVariantOptionValueWriteRepository vovw,
        IProductReadRepository pr,
        IProductWriteRepository pw,
        IProductOptionValueReadRepository ovr,
        IMapper mapper,
        IAuditHelper audit)
    {
        _vr = vr;
        _vw = vw;
        _colorReadRepository = colorReadRepository;
        _vovr = vovr;
        _vovw = vovw;
        _pr = pr;
        _pw = pw;
        _ovr = ovr;
        _mapper = mapper;
        _audit = audit;
    }

    // ======================================================
    //                      GET BY ID
    // ======================================================
    public async Task<ProductVariantDto> GetByIdAsync(string id)
    {
        var gid = EnsureGuid(id);

        var entity = await _vr.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(x => x.OptionValues)
                  .ThenInclude(v => v.OptionValue).Include(x=>x.ProductColor).
                  ThenInclude(x=>x.Product).Include(x=>x.Product)
        ) ?? throw new GlobalAppException("PRODUCT_VARIANT_NOT_FOUND");

        return _mapper.Map<ProductVariantDto>(entity);
    }
    public async Task<ProductVariantDto> GetBySlugAsync(string slug)
    {
     

        var entity = await _vr.GetAsync(
            x => x.VariantSlug == slug && !x.IsDeleted,
            q => q.Include(x => x.OptionValues)
                .ThenInclude(v => v.OptionValue).Include(x=>x.ProductColor).
                ThenInclude(x=>x.Product).Include(x=>x.Product)
        ) ?? throw new GlobalAppException("PRODUCT_VARIANT_NOT_FOUND");

        return _mapper.Map<ProductVariantDto>(entity);
    }

    // ======================================================
    //                   GET BY PRODUCT
    // ======================================================
    public async Task<List<ProductVariantDto>> GetByProductAsync(string productId)
    {
        var pid = EnsureGuid(productId);

        var list = await _vr.GetAllAsync(
            x => x.ProductId == pid && !x.IsDeleted,
            q => q.Include(x => x.OptionValues)
                  .ThenInclude(v => v.OptionValue).Include(x=>x.ProductColor).
                  ThenInclude(x=>x.Product).Include(x=>x.Product)
        );

        return _mapper.Map<List<ProductVariantDto>>(list);
    }

    // ======================================================
    //                      CREATE
    // ======================================================
public async Task CreateAsync(CreateProductVariantDto dto)
{
    // 1. VALIDATION
    if (dto.ProductId == null && dto.ProductColorId == null)
        throw new GlobalAppException("PRODUCT_OR_COLOR_NOT_FOUND");

    if (dto.OptionValueIds == null || dto.OptionValueIds.Count == 0)
        throw new GlobalAppException("INVALID_OPTION_VALUE_ID");


    Guid productId;
    Guid? colorId = null;

    // 2. PRODUCT-LEVEL VARIANT
    if (dto.ProductId != null)
    {
        productId = EnsureGuid(dto.ProductId);

        var product = await _pr.GetAsync(x => x.Id == productId && !x.IsDeleted)
            ?? throw new GlobalAppException("PRODUCT_NOT_FOUND");
    }
    // 3. COLOR-LEVEL VARIANT (WITHOUT PRODUCT ID SENT)
    else
    {
        // ProductColorId var → o rəngin product-ını tapırıq
        colorId = EnsureGuid(dto.ProductColorId);

        var color = await _colorReadRepository.GetAsync(x => x.Id == colorId && !x.IsDeleted)
            ?? throw new GlobalAppException("PRODUCT_COLOR_NOT_FOUND");

        productId = color.ProductId; // 🔥 productId buradan çıxır
    }



    // CREATE ENTITY
    var variant = _mapper.Map<ProductVariant>(dto);
    // SKU provided?
    string sku;

    if (!string.IsNullOrWhiteSpace(dto.Sku))
    {
        await EnsureSkuUnique(dto.Sku, null);
        sku = dto.Sku.Trim();
    }
    else
    {
        // AUTO GENERATE SKU
        sku = await GenerateVariantSkuAsync(productId, colorId, dto.OptionValueIds);
    }

    variant.Sku = sku;
    variant.Id = Guid.NewGuid();
    variant.ProductId = productId;
    variant.ProductColorId = colorId;
    variant.CreatedDate = variant.LastUpdatedDate = DateTime.UtcNow;
    variant.IsAvailable = true;
    variant.IsDeleted = false;

    // SLUG GENERATION (colorId-ni də əlavə edirik!)
    variant.VariantSlug = await GenerateVariantSlugAsync(
        dto.VariantSlug,
        productId,
        colorId,
        dto.OptionValueIds
    );

    await _vw.AddAsync(variant);

    // ADD OPTION VALUES
    foreach (var idStr in dto.OptionValueIds)
    {
        var ovId = EnsureGuid(idStr);

        var val = await _ovr.GetAsync(x => x.Id == ovId && !x.IsDeleted)
            ?? throw new GlobalAppException("OPTION_VALUE_NOT_FOUND");

        await _vovw.AddAsync(new ProductVariantOptionValue
        {
            Id = Guid.NewGuid(),
            VariantId = variant.Id,
            OptionValueId = ovId,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        });
    }

    await _vw.CommitAsync();

    await RefreshProductStock(productId);
    await _audit.LogAsync(
        "ProductVariants",
        "CREATE",
        "PRODUCT_VARIANT_CREATED",
        variant.Id,
        null,
        new Dictionary<string, object>
        {
            ["ProductId"] = variant.ProductId.ToString(),
            ["VariantSlug"] = variant.VariantSlug,
            ["ProductName"]=variant.Product.NameAz,
            ["ProductColorName"]=variant.ProductColor.ColorNameAz?? " ",
            ["ProductColorId"] = variant.ProductColorId?.ToString() ?? "",
            ["Sku"] = variant.Sku ?? "",
            ["StockQuantity"] = variant.StockQuantity
        }
    );
    await _audit.LogAsync(
        "Products",
        "UPDATE",
        "PRODUCT_BUILD_COMPLETED",
        productId,
        null,
        new Dictionary<string, object>
        {
            ["ProductId"] = productId.ToString(),
            ["TriggeredBy"] = "VariantCreated"
        }
    );
}

    // ======================================================
    //                      UPDATE
    // ======================================================
public async Task UpdateAsync(UpdateProductVariantDto dto)
{
    var gid = EnsureGuid(dto.Id);

    var variant = await _vr.GetAsync(
        x => x.Id == gid && !x.IsDeleted,
        q => q.Include(v => v.OptionValues),
        enableTracking: true
    ) ?? throw new GlobalAppException("PRODUCT_VARIANT_NOT_FOUND");
    var oldValues = new Dictionary<string, object>
    {
        ["ProductId"] = variant.ProductId.ToString(),
        ["VariantSlug"] = variant.VariantSlug,
        ["ProductName"] = variant.Product.NameAz,
        ["ProductColorName"] = variant.ProductColor.ColorNameAz ?? " ",
        ["ProductColorId"] = variant.ProductColorId?.ToString() ?? "",
        ["Sku"] = variant.Sku ?? "",
        ["StockQuantity"] = variant.StockQuantity
    };
    // ==========================
    // 1) SKU uniqueness
    // ==========================
    if (!string.IsNullOrWhiteSpace(dto.Sku))
        await EnsureSkuUnique(dto.Sku, gid);

    // ==========================
    // 2) COLOR / PRODUCT RELATION LOGIC
    // ==========================
    Guid? colorId = variant.ProductColorId; // default: existing color
    Guid productId = variant.ProductId;     // default: existing product
    

    // ----- Case 1: ProductColorId DƏYİŞİLİB -----
    if (dto.ProductColorId != null)
    {
        if (string.IsNullOrWhiteSpace(dto.ProductColorId))
        {
            // null → product-level variant
            colorId = null;
        }
        else
        {
            // new color variant
            colorId = EnsureGuid(dto.ProductColorId);

            var colorEntity = await _colorReadRepository.GetAsync(
                x => x.Id == colorId && !x.IsDeleted
            ) ?? throw new GlobalAppException("PRODUCT_COLOR_NOT_FOUND");

            // color → productId həmişə rəngdən gəlir
            productId = colorEntity.ProductId;
        }
    }

    // ----- Case 2: dto.ProductColorId null gəlməyibsə → rəng dəyişmir
    // productId variant.ProductId olaraq qalır

    // ==========================
    // 3) OptionValue list to use
    // ==========================
    List<string> optionValueIds = dto.OptionValueIds ??
        variant.OptionValues.Select(x => x.OptionValueId.ToString()).ToList();

    // ==========================
    // 4) SLUG REBUILD ONLY IF DTO BRINGS NULL
    // ==========================
    if (dto.VariantSlug == null)
    {
        dto.VariantSlug = await GenerateVariantSlugAsync(
            null,
            productId,
            colorId,
            optionValueIds
        );
        
    }
    if (!string.IsNullOrWhiteSpace(dto.Sku))
    {
        await EnsureSkuUnique(dto.Sku, gid);
        variant.Sku = dto.Sku.Trim();
    }
    else
    {
        // auto-generate new SKU if dto.Sku NULL (rare case but valid)
        variant.Sku = await GenerateVariantSkuAsync(productId, colorId, optionValueIds);
    }

    // ==========================
    // 5) Map DTO → Variant
    // ==========================
    _mapper.Map(dto, variant);

    variant.ProductColorId = colorId;
    variant.ProductId = productId;
    variant.LastUpdatedDate = DateTime.UtcNow;

    // ==========================
    // 6) OPTION VALUES UPSERT
    // ==========================
    if (dto.OptionValueIds != null)
    {
        // old values → mark deleted
        foreach (var oldVal in variant.OptionValues)
        {
            oldVal.IsDeleted = true;
            oldVal.DeletedDate = DateTime.UtcNow;
        }

        // new values
        foreach (var valIdStr in dto.OptionValueIds)
        {
            var ovId = EnsureGuid(valIdStr);

            await _vovw.AddAsync(new ProductVariantOptionValue
            {
                Id = Guid.NewGuid(),
                VariantId = variant.Id,
                OptionValueId = ovId,
                CreatedDate = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow,
                IsDeleted = false
            });
        }
    }

    // ==========================
    // 7) SAVE
    // ==========================
    await _vw.CommitAsync();

    // ==========================
    // 8) REFRESH PRODUCT STOCK
    // ==========================
    await RefreshProductStock(productId);
    await _audit.LogAsync(
        "ProductVariants",
        "UPDATE",
        "PRODUCT_VARIANT_UPDATED",
        variant.Id,
        oldValues,
        new Dictionary<string, object>
        {
            ["ProductId"] = variant.ProductId.ToString(),
            ["VariantSlug"] = variant.VariantSlug,
            ["ProductName"] = variant.Product.NameAz,
            ["ProductColorName"] = variant.ProductColor.ColorNameAz ?? " ",
            ["ProductColorId"] = variant.ProductColorId?.ToString() ?? "",
            ["Sku"] = variant.Sku ?? "",
            ["StockQuantity"] = variant.StockQuantity
        }
    );
}

    // ======================================================
    //                      DELETE
    // ======================================================
    public async Task DeleteAsync(string id)
    {
        var gid = EnsureGuid(id);

        var variant = await _vr.GetAsync(x => x.Id == gid && !x.IsDeleted, enableTracking: true)
            ?? throw new GlobalAppException("PRODUCT_VARIANT_NOT_FOUND");

        variant.IsDeleted = true;
        variant.DeletedDate = DateTime.UtcNow;

        await _vw.CommitAsync();
        await RefreshProductStock(variant.ProductId);
        await _audit.LogAsync(
            "ProductVariants",
            "DELETE",
            "PRODUCT_VARIANT_DELETED",
            variant.Id,
            new Dictionary<string, object>
            {
                ["ProductId"] = variant.ProductId.ToString(),
                ["VariantSlug"] = variant.VariantSlug,
                ["ProductName"] = variant.Product.NameAz,
                ["ProductColorName"] = variant.ProductColor.ColorNameAz ?? " ",
                ["ProductColorId"] = variant.ProductColorId?.ToString() ?? "",
                ["Sku"] = variant.Sku ?? "",
                ["StockQuantity"] = variant.StockQuantity
            },
            null
        );
    }

    // ======================================================
    //           PRODUCT STOCK AGGREGATION
    // ======================================================
    private async Task RefreshProductStock(Guid productId)
    {
        var product = await _pr.GetAsync(x => x.Id == productId && !x.IsDeleted, enableTracking: true);

        // Product.StockQuantity yalnız NULL-dursa variant toplamından doldurulmalıdır
        if (product != null && product.StockQuantity == null)
        {
            var variants = await _vr.GetAllAsync(x => x.ProductId == productId && !x.IsDeleted);

            product.StockQuantity = variants.Sum(v => v.StockQuantity);
            product.LastUpdatedDate = DateTime.UtcNow;

            await _pw.CommitAsync();
        }
    }
    private async Task<string> GenerateVariantSkuAsync(
        Guid productId,
        Guid? colorId,
        List<string> optionValueIds)
    {
        // 1) Base SKU prefix — product SKU
        var product = await _pr.GetAsync(x => x.Id == productId && !x.IsDeleted)
                      ?? throw new GlobalAppException("PRODUCT_NOT_FOUND");

        string baseSku = Slugify(product.Sku);

        // 2) OptionValues → SKU part
        var optionValues = await _ovr.GetAllAsync(
            x => optionValueIds.Contains(x.Id.ToString()) && !x.IsDeleted);

        string optionSku = string.Join("-", optionValues.Select(v => Slugify(v.ValueAz)));

        // 3) Color → SKU part
        string colorSku = "";
        if (colorId.HasValue)
        {
            var color = await _colorReadRepository.GetAsync(x => x.Id == colorId && !x.IsDeleted);
            if (color != null)
                colorSku = Slugify(color.ColorNameAz ?? "");
        }

        // 4) Full Base SKU
        string fullBaseSku =
            string.IsNullOrWhiteSpace(colorSku)
                ? $"{baseSku}-{optionSku}"
                : $"{baseSku}-{colorSku}-{optionSku}";

        fullBaseSku = fullBaseSku.Trim('-');

        // 5) Uniqueness
        string candidate = fullBaseSku;
        int i = 2;

        while (true)
        {
            var exists = await _vr.GetAsync(x =>
                x.ProductId == productId &&
                x.Sku == candidate &&
                !x.IsDeleted);

            if (exists == null)
                return candidate;

            candidate = $"{fullBaseSku}-{i}";
            i++;
        }
    }

    // ======================================================
    //                   SKU UNIQUE CHECK
    // ======================================================
    private async Task EnsureSkuUnique(string sku, Guid? ignoreId)
    {
        var exists = await _vr.GetAsync(x =>
            x.Sku == sku &&
            !x.IsDeleted &&
            (ignoreId == null || x.Id != ignoreId.Value));

        if (exists != null)
            throw new GlobalAppException("VARIANT_SKU_EXISTS");
    }

    // ======================================================
    //                   SLUG GENERATOR
    // ======================================================
    private async Task<string> GenerateVariantSlugAsync(
        string? customSlug,
        Guid productId,
        Guid? colorId,
        List<string> optionValueIds
    )
    {
        // 1) OPTION VALUES SLUG
        var optionValues = await _ovr.GetAllAsync(
            x => optionValueIds.Contains(x.Id.ToString()) && !x.IsDeleted);

        string optionsSlug = Slugify(string.Join("-", optionValues.Select(v => v.ValueAz)));

        // 2) COLOR SLUG (optional)
        string colorSlug = "";
        if (colorId.HasValue)
        {
            var color = await _colorReadRepository.GetAsync(
                x => x.Id == colorId && !x.IsDeleted);

            if (color != null)
                colorSlug = Slugify(color.ColorNameAz ?? "");
        }

        // 3) BASE SLUG (customSlug üstünlük təşkil edir)
        string baseSlug;

        if (!string.IsNullOrWhiteSpace(customSlug))
        {
            baseSlug = Slugify(customSlug);
        }
        else
        {
            baseSlug = string.IsNullOrWhiteSpace(colorSlug)
                ? optionsSlug                                 // product variant
                : $"{colorSlug}-{optionsSlug}";               // color variant
        }

        // 4) Uniqueness check — productId + colorId birlikdə
        string candidate = baseSlug;
        int i = 2;

        while (true)
        {
            var exists = await _vr.GetAsync(x =>
                x.ProductId == productId &&
                x.ProductColorId == colorId &&     // 🔥 IMPORTANT!!
                x.VariantSlug == candidate &&
                !x.IsDeleted
            );

            if (exists == null)
                return candidate;

            candidate = $"{baseSlug}-{i}";
            i++;
        }
    }

    // ======================================================
    //                      HELPERS
    // ======================================================
    private static string Slugify(string text)
    {
        text = text.ToLowerInvariant().Trim();
        var sb = new System.Text.StringBuilder();

        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else
                sb.Append('-');
        }

        var result = sb.ToString().Trim('-');
        while (result.Contains("--"))
            result = result.Replace("--", "-");

        return result;
    }

    private Guid EnsureGuid(string id)
    {
        if (!Guid.TryParse(id, out var gid))
            throw new GlobalAppException("INVALID_ID_FORMAT");
        return gid;
    }
}