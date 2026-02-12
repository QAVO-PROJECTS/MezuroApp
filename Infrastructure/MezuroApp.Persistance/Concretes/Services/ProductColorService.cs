using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.ProductColors;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductImages;
using MezuroApp.Application.Abstracts.Repositories.ProductColorImages;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductColor;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

public class ProductColorService : IProductColorService
{
    private readonly IProductColorReadRepository _readRepo;
    private readonly IProductColorWriteRepository _writeRepo;
    private readonly IProductReadRepository _productReadRepo;
    private readonly IProductImageReadRepository _imageReadRepo;
    private readonly IProductColorImageReadRepository _pciReadRepo;
    private readonly IProductColorImageWriteRepository _pciWriteRepo;
    private readonly IMapper _mapper;

    public ProductColorService(
        IProductColorReadRepository readRepo,
        IProductColorWriteRepository writeRepo,
        IProductReadRepository productReadRepo,
        IProductImageReadRepository imageReadRepo,
        IProductColorImageReadRepository pciReadRepo,
        IProductColorImageWriteRepository pciWriteRepo,
        IMapper mapper)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _productReadRepo = productReadRepo;
        _imageReadRepo = imageReadRepo;
        _pciReadRepo = pciReadRepo;
        _pciWriteRepo = pciWriteRepo;
        _mapper = mapper;
    }

    // =============================
    //            QUERIES
    // =============================
    public async Task<ProductColorDto> GetByIdAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);
        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q
                .Include(pc => pc.ColorImages.Where(ci => !ci.IsDeleted))
                    .ThenInclude(ci => ci.ProductImage)
                .Include(pc => pc.ColorVariants.Where(v => !v.IsDeleted))
        );

        if (entity == null) throw new GlobalAppException("PRODUCT_COLOR_NOT_FOUND");

        return _mapper.Map<ProductColorDto>(entity);
    }

    public async Task<List<ProductColorDto>> GetAllAsync(string productId)
    {
        var pid = ParseGuidOrThrow(productId);

        var productExists = await _productReadRepo.GetAsync(
            x => x.Id == pid && !x.IsDeleted
        ) ?? throw new GlobalAppException("PRODUCT_NOT_FOUND");

        var entities = await _readRepo.GetAllAsync(
            x => !x.IsDeleted && x.ProductId == pid,
            q => q
                .Include(pc => pc.ColorImages.Where(ci => !ci.IsDeleted))
                    .ThenInclude(ci => ci.ProductImage)
                .Include(pc => pc.ColorVariants.Where(v => !v.IsDeleted))
        );

        return _mapper.Map<List<ProductColorDto>>(entities);
    }

    // =============================
    //            COMMANDS
    // =============================
    public async Task CreateAsync(CreateProductColorDto dto)
    {
        var pid = ParseGuidOrThrow(dto.ProductId);

        // 1) Məhsulu yoxla
        var product = await _productReadRepo.GetAsync(x => x.Id == pid && !x.IsDeleted)
            ?? throw new GlobalAppException("PRODUCT_NOT_FOUND");

        // 2) SKU: gəlməyibsə auto-generate et; gəlmişsə validate et
        var incomingSku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku!.Trim();
        if (incomingSku == null)
        {
            incomingSku = await GenerateUniqueColorSkuAsync(product, dto);
        }
        else
        {
            await EnsureColorUniquenessAsync(pid, incomingSku, dto.ColorCode, null);
        }

        // 3) Entity map
        var entity = _mapper.Map<ProductColor>(dto);
        entity.Id = Guid.NewGuid();
        entity.ProductId = pid;
        entity.Sku = incomingSku;
        entity.CreatedDate = DateTime.UtcNow;
        entity.LastUpdatedDate = entity.CreatedDate;
        entity.IsDeleted = false;

        await _writeRepo.AddAsync(entity);
        await _writeRepo.CommitAsync();

        // 4) ColorImage linkləri (toplu, N+1-siz)
        if (dto.ColorImageIds != null && dto.ColorImageIds.Count > 0)
        {
            var imgGuids = dto.ColorImageIds
                .Where(x=>Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

            if (imgGuids.Count > 0)
            {
                // yalnız həmin məhsula aid şəkilləri bağla
                var images = await _imageReadRepo.GetAllAsync(
                    x => !x.IsDeleted && x.ProductId == pid && imgGuids.Contains(x.Id)
                );

                var imageIds = images.Select(i => i.Id).ToList();

                var existingLinks = await _pciReadRepo.GetAllAsync(
                    x => x.ProductColorId == entity.Id && imageIds.Contains(x.ProductImageId)
                );
                var existingIds = existingLinks.Select(l => l.ProductImageId).ToHashSet();

                var toAdd = imageIds
                    .Where(imgId => !existingIds.Contains(imgId))
                    .Select(imgId => new ProductColorImage
                    {
                        Id = Guid.NewGuid(),
                        ProductColorId = entity.Id,
                        ProductImageId = imgId,
                        CreatedDate = DateTime.UtcNow,
                        LastUpdatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    })
                    .ToList();

                foreach (var link in toAdd)
                    await _pciWriteRepo.AddAsync(link);

                if (toAdd.Count > 0)
                    await _pciWriteRepo.CommitAsync();
            }
        }
    }

    public async Task UpdateAsync(UpdateProductColorDto dto)
    {
        var gid = ParseGuidOrThrow(dto.Id);

        // 1) tracking ilə yüklə
        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(pc => pc.ColorImages.Where(ci => !ci.IsDeleted)),
            enableTracking: true
        ) ?? throw new GlobalAppException("PRODUCT_COLOR_NOT_FOUND");

        // 2) SKU davranışı:
        // - dto.Sku == null → SKU-ya toxunma
        // - dto.Sku == ""  → yenidən auto-generate et
        // - dto.Sku != ""  → unikallıq yoxla və təyin et
        if (dto.Sku != null)
        {
            var trimmed = dto.Sku.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                // regenerate
                var product = await _productReadRepo.GetAsync(x => x.Id == entity.ProductId && !x.IsDeleted)
                    ?? throw new GlobalAppException("PRODUCT_NOT_FOUND");

                entity.Sku = await GenerateUniqueColorSkuAsync(product, new CreateProductColorDto
                {
                    // auto generation üçün lazım ola biləcək sahələr
                    ColorCode = dto.ColorCode ?? entity.ColorCode,
                    ColorNameEn = dto.ColorNameEn ?? entity.ColorNameEn,
                    ColorNameAz = dto.ColorNameAz ?? entity.ColorNameAz,
                    ColorNameTr = dto.ColorNameTr ?? entity.ColorNameTr,
                    ColorNameRu = dto.ColorNameRu ?? entity.ColorNameRu
                });
            }
            else
            {
                await EnsureColorUniquenessAsync(entity.ProductId, trimmed, dto.ColorCode ?? entity.ColorCode, entity.Id);
                entity.Sku = trimmed;
            }
        }

        // 3) Unikallıq (ColorCode) – dəyişirsə
        if (dto.ColorCode != null)
        {
            await EnsureColorUniquenessAsync(entity.ProductId, entity.Sku, dto.ColorCode, entity.Id);
        }

        // 4) Qalan sahələr partial update
        _mapper.Map(dto, entity);
        entity.LastUpdatedDate = DateTime.UtcNow;

        // 5) Yeni linklər (re-activate/insert) – toplu
        if (dto.NewColorImageIds != null && dto.NewColorImageIds.Count > 0)
        {
            var imgGuids = dto.NewColorImageIds
                .Where(x=>Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

            if (imgGuids.Count > 0)
            {
                var validImages = await _imageReadRepo.GetAllAsync(
                    x => !x.IsDeleted && x.ProductId == entity.ProductId && imgGuids.Contains(x.Id)
                );
                var validIds = validImages.Select(i => i.Id).ToList();

                var existingLinks = await _pciReadRepo.GetAllAsync(
                    x => x.ProductColorId == entity.Id && validIds.Contains(x.ProductImageId)
                );
                var existingByImageId = existingLinks.ToDictionary(l => l.ProductImageId, l => l);

                var toAdd = new List<ProductColorImage>();
                foreach (var imgId in validIds)
                {
                    if (!existingByImageId.TryGetValue(imgId, out var link))
                    {
                        toAdd.Add(new ProductColorImage
                        {
                            Id = Guid.NewGuid(),
                            ProductColorId = entity.Id,
                            ProductImageId = imgId,
                            CreatedDate = DateTime.UtcNow,
                            LastUpdatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        });
                    }
                    else if (link.IsDeleted)
                    {
                        link.IsDeleted = false;
                        link.LastUpdatedDate = DateTime.UtcNow;
                    }
                }

                foreach (var link in toAdd) await _pciWriteRepo.AddAsync(link);
                if (toAdd.Count > 0) await _pciWriteRepo.CommitAsync();
            }
        }

        // 6) Silinən linklər (soft delete)
        if (dto.DeletedColorImageIds != null && dto.DeletedColorImageIds.Count > 0)
        {
            var delGuids = dto.DeletedColorImageIds
                .Where(x=>Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .ToList();

            var toDeleteLinks = entity.ColorImages?
                .Where(ci => !ci.IsDeleted && delGuids.Contains(ci.ProductImageId))
                .ToList() ?? new List<ProductColorImage>();

            foreach (var link in toDeleteLinks)
            {
                link.IsDeleted = true;
                link.DeletedDate = DateTime.UtcNow;
            }
        }

        await _writeRepo.CommitAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);
        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(pc => pc.ColorImages.Where(ci => !ci.IsDeleted)),
            enableTracking: true
        ) ?? throw new GlobalAppException("PRODUCT_COLOR_NOT_FOUND");

        // Linkləri soft-delete
        if (entity.ColorImages != null)
        {
            foreach (var link in entity.ColorImages.Where(ci => !ci.IsDeleted))
            {
                link.IsDeleted = true;
                link.DeletedDate = DateTime.UtcNow;
            }
        }

        // Rəngi soft-delete
        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }

    // =============================
    //            HELPERS
    // =============================
    private static Guid ParseGuidOrThrow(string id)
    {
        if (!Guid.TryParse(id, out var gid))
            throw new GlobalAppException("INVALID_ID_FORMAT");
        return gid;
    }

    /// SKU və ColorCode-un eyni məhsul daxilində unikallığını yoxlayır
    private async Task EnsureColorUniquenessAsync(Guid productId, string? sku, string? colorCode, Guid? currentId)
    {
        if (!string.IsNullOrWhiteSpace(sku))
        {
            var skuExists = await _readRepo.GetAsync(x =>
                !x.IsDeleted && x.ProductId == productId && x.Sku == sku &&
                (currentId == null || x.Id != currentId.Value)
            );
            if (skuExists != null) throw new GlobalAppException("COLOR_SKU_ALREADY_EXISTS");
        }

        if (!string.IsNullOrWhiteSpace(colorCode))
        {
            var codeExists = await _readRepo.GetAsync(x =>
                !x.IsDeleted && x.ProductId == productId && x.ColorCode == colorCode &&
                (currentId == null || x.Id != currentId.Value)
            );
            if (codeExists != null) throw new GlobalAppException("COLOR_CODE_ALREADY_EXISTS");
        }
    }

    /// SKU auto-generate (product.Sku + color token) və unikal suffix
    private async Task<string> GenerateUniqueColorSkuAsync(Product product, CreateProductColorDto dto)
    {
        var basePart = !string.IsNullOrWhiteSpace(product.Sku)
            ? SanitizeSkuPart(product.Sku)
            : product.Id.ToString("N")[..8].ToUpperInvariant();

        // Token prioriteti: ColorCode → ColorNameEn → ColorNameAz → ColorNameTr → ColorNameRu → "CLR"
        var token = !string.IsNullOrWhiteSpace(dto.ColorCode)
            ? SanitizeSkuPart(dto.ColorCode!.Replace("#", ""))
            : SlugifyFirstNonEmpty(dto.ColorNameEn, dto.ColorNameAz, dto.ColorNameTr, dto.ColorNameRu);

        if (string.IsNullOrWhiteSpace(token))
            token = "CLR";

        var candidate = $"{basePart}-{token}".ToUpperInvariant();

        // konfliktdə suffix əlavə et: -C2, -C3...
        var unique = await MakeSkuUniqueWithinProductAsync(product.Id, candidate);
        return unique;
    }

    private async Task<string> MakeSkuUniqueWithinProductAsync(Guid productId, string candidate)
    {
        // əvvəlcə özünü yoxla
        var exists = await _readRepo.GetAsync(x =>
            !x.IsDeleted && x.ProductId == productId && x.Sku == candidate
        );

        if (exists == null) return candidate;

        int i = 2;
        while (true)
        {
            var next = $"{candidate}-C{i}";
            var hit = await _readRepo.GetAsync(x =>
                !x.IsDeleted && x.ProductId == productId && x.Sku == next
            );
            if (hit == null) return next;
            i++;
        }
    }

    private static string SlugifyFirstNonEmpty(params string?[] values)
    {
        var v = values.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();
        if (string.IsNullOrWhiteSpace(v)) return "";

        // diakritikanı təmizlə + alfanumerik + tire
        v = RemoveDiacritics(v);
        var sb = new StringBuilder();
        foreach (var ch in v)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToUpperInvariant(ch));
            else if (char.IsWhiteSpace(ch) || ch == '_' || ch == '-' || ch == '/') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "" : slug;
    }

    private static string SanitizeSkuPart(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        input = RemoveDiacritics(input);
        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToUpperInvariant(ch));
            else if (ch == '-' || ch == '_' ) sb.Append('-');
        }
        var res = sb.ToString().Trim('-');
        // çox uzundursa bir az qısaltmaq olar
        if (res.Length > 24) res = res[..24];
        return res;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: normalized.Length);
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}