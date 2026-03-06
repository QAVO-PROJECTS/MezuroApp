using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.ProductOptions;
using MezuroApp.Application.Abstracts.Repositories.OptionValues;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductVariantOptionValues;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductOption;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class ProductOptionService : IProductOptionService
{
    private readonly IProductOptionReadRepository _readRepo;
    private readonly IProductOptionWriteRepository _writeRepo;
    private readonly IProductOptionValueReadRepository _valueReadRepo;
    private readonly IProductOptionValueWriteRepository _valueWriteRepo;
    private readonly IProductVariantOptionValueReadRepository _variantValueReadRepo;
    private readonly IProductReadRepository _productReadRepo;
    private readonly IMapper _mapper;
    private readonly IAuditHelper _audit;

    public ProductOptionService(
        IProductOptionReadRepository readRepo,
        IProductOptionWriteRepository writeRepo,
        IProductOptionValueReadRepository valueReadRepo,
        IProductOptionValueWriteRepository valueWriteRepo,
        IProductVariantOptionValueReadRepository variantValueReadRepo,
        IProductReadRepository productReadRepo,
        IMapper mapper,
        IAuditHelper audit)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _valueReadRepo = valueReadRepo;
        _valueWriteRepo = valueWriteRepo;
        _variantValueReadRepo = variantValueReadRepo;
        _productReadRepo = productReadRepo;
        _mapper = mapper;
        _audit = audit;
    }

    public async Task<ProductOptionDto> GetByIdAsync(string id)
    {
        var gid = ParseGuid(id);

        var option = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(x => x.Values).Include(x => x.Option)
        ) ?? throw new GlobalAppException("PRODUCT_OPTION_NOT_FOUND");

        return _mapper.Map<ProductOptionDto>(option);
    }

    public async Task<List<ProductOptionDto>> GetByProductAsync(string productId)
    {
        var pid = ParseGuid(productId);

        var list = await _readRepo.GetAllAsync(
            x => !x.IsDeleted && x.ProductId == pid,
            q => q.Include(x => x.Values).Include(x => x.Option)
        );

        return _mapper.Map<List<ProductOptionDto>>(list);
    }

    public async Task CreateAsync(CreateProductOptionDto dto)
    {
        var productId = ParseGuid(dto.ProductId);
        var optionId = ParseGuid(dto.OptionId);

        // Product mövcuddur?
        _ = await _productReadRepo.GetAsync(x => x.Id == productId && !x.IsDeleted)
            ?? throw new GlobalAppException("PRODUCT_NOT_FOUND");

        // Eyni Option məhsula təkrar əlavə olunmamalıdır
        var exists = await _readRepo.GetAsync(x =>
            x.ProductId == productId &&
            x.OptionId == optionId &&
            !x.IsDeleted);

        if (exists != null)
            throw new GlobalAppException("PRODUCT_OPTION_ALREADY_EXISTS");

        // Entity yarat
        var entity = new ProductOption
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            OptionId = optionId,
            CustomNameAz = dto.CustomNameAz,
            CustomNameEn = dto.CustomNameEn,
            CustomNameRu = dto.CustomNameRu,
            CustomNameTr = dto.CustomNameTr,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        // Value-ları yarat (id = null → create)
        if (dto.Values != null)
        {
            entity.Values = new List<ProductOptionValue>();

            foreach (var v in dto.Values)
            {
                var valueAz = EnsureNonEmpty(v.ValueAz, "VALUE_AZ_REQUIRED");

                var duplicate = entity.Values.Any(x =>
                    !x.IsDeleted && x.ValueAz.ToLower() == valueAz.ToLower());

                if (duplicate)
                    throw new GlobalAppException("OPTION_VALUE_ALREADY_EXISTS");

                entity.Values.Add(new ProductOptionValue
                {
                    Id = Guid.NewGuid(),
                    OptionId = entity.Id,
                    ValueAz = valueAz,
                    ValueEn = v.ValueEn ?? valueAz,
                    ValueRu = v.ValueRu ?? valueAz,
                    ValueTr = v.ValueTr ?? valueAz,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    IsDeleted = false
                });
            }
        }

        await _writeRepo.AddAsync(entity);
        await _writeRepo.CommitAsync();
        await _audit.LogAsync(
            "ProductOptions",
            "CREATE",
            "PRODUCT_OPTION_ADDED",
            entity.Id,
            null,
            new Dictionary<string, object>
            {
                ["ProductId"] = entity.ProductId.ToString(),
                ["OptionId"] = entity.OptionId,
                ["CustomNameAz"] = entity.CustomNameAz ?? ""
            }
        );
    }

    public async Task UpdateAsync(UpdateProductOptionDto dto)
    {
        var gid = ParseGuid(dto.Id);

        var option = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(x => x.Values),
            enableTracking: true
        ) ?? throw new GlobalAppException("PRODUCT_OPTION_NOT_FOUND");
        var oldValues = new Dictionary<string, object>
        {
            ["CustomNameAz"] = option.CustomNameAz ?? "",
            ["ProductId"] = option.ProductId
            
        };

        // Update metadata
        _mapper.Map(dto, option);
        option.LastUpdatedDate = DateTime.UtcNow;

        option.Values ??= new List<ProductOptionValue>();

        // ===================== DELETE VALUES =====================
        if (dto.DeleteValueIds != null)
        {
            foreach (var idStr in dto.DeleteValueIds)
            {
                if (!Guid.TryParse(idStr, out var deleteId))
                    throw new GlobalAppException("INVALID_OPTION_VALUE_ID");

                // variant-da istifadə edilirsə silinməz
                var inUse = await _variantValueReadRepo.GetAsync(v => v.OptionValueId == deleteId);

                if (inUse!=null)
                    throw new GlobalAppException("OPTION_VALUE_IN_USE");

                var val = option.Values.FirstOrDefault(x => x.Id == deleteId && !x.IsDeleted);

                if (val != null)
                {
                    val.IsDeleted = true;
                    val.DeletedDate = DateTime.UtcNow;
                    val.LastUpdatedDate = DateTime.UtcNow;
                }
            }
        }

        // ===================== UPSERT VALUES =====================
        if (dto.Values != null)
        {
            foreach (var v in dto.Values)
            {
                var valueAz = EnsureNonEmpty(v.ValueAz, "VALUE_AZ_REQUIRED");

                if (string.IsNullOrWhiteSpace(v.Id))
                {
                    // NEW VALUE
                    var duplicate = option.Values.Any(x =>
                        !x.IsDeleted && x.ValueAz.ToLower() == valueAz.ToLower());

                    if (duplicate)
                        throw new GlobalAppException("OPTION_VALUE_ALREADY_EXISTS");

                    option.Values.Add(new ProductOptionValue
                    {
                        Id = Guid.NewGuid(),
                        OptionId = option.Id,
                        ValueAz = valueAz,
                        ValueEn = v.ValueEn ?? valueAz,
                        ValueRu = v.ValueRu ?? valueAz,
                        ValueTr = v.ValueTr ?? valueAz,
                        CreatedDate = DateTime.UtcNow,
                        LastUpdatedDate = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
                else
                {
                    // UPDATE EXISTING
                    if (!Guid.TryParse(v.Id, out var valueId))
                        throw new GlobalAppException("INVALID_OPTION_VALUE_ID");

                    var existing = option.Values.FirstOrDefault(
                        x => x.Id == valueId && !x.IsDeleted
                    ) ?? throw new GlobalAppException("OPTION_VALUE_NOT_FOUND");

                    // Dublikat check
                    var duplicate = option.Values.Any(x =>
                        x.Id != existing.Id &&
                        !x.IsDeleted &&
                        x.ValueAz.ToLower() == valueAz.ToLower());

                    if (duplicate)
                        throw new GlobalAppException("OPTION_VALUE_ALREADY_EXISTS");

                    _mapper.Map(v, existing);
                    existing.LastUpdatedDate = DateTime.UtcNow;
                }
            }
        }

        await _writeRepo.CommitAsync();
        await _audit.LogAsync(
            "ProductOptions",
            "UPDATE",
            "PRODUCT_OPTION_UPDATED",
            option.Id,
            oldValues,
            new Dictionary<string, object>
            {
                ["CustomNameAz"] = option.CustomNameAz ?? "",
                ["ProductId"] = option.ProductId
            }
        );
    }



    public async Task DeleteAsync(string id)
    {
        var gid = ParseGuid(id);

        var option = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(x => x.Values),
            enableTracking: true
        ) ?? throw new GlobalAppException("PRODUCT_OPTION_NOT_FOUND");

        // Soft delete
        option.IsDeleted = true;
        option.DeletedDate = DateTime.UtcNow;

        foreach (var v in option.Values.Where(x => !x.IsDeleted))
        {
            // variant-da istifadə edilirsə silinməz
            var inUse = await _variantValueReadRepo.GetAsync(x => x.OptionValueId == v.Id);

            if (inUse!=null)
                throw new GlobalAppException("OPTION_VALUE_IN_USE");

            v.IsDeleted = true;
            v.DeletedDate = option.DeletedDate;
            v.LastUpdatedDate = option.DeletedDate;
        }

        await _writeRepo.CommitAsync();
        await _audit.LogAsync(
            "ProductOptions",
            "DELETE",
            "PRODUCT_OPTION_DELETED",
            option.Id,
            new Dictionary<string, object>
            {
                ["ProductId"] = option.ProductId,
                ["OptionId"] = option.OptionId
            },
            null
        );
    }

    // Helpers
    private Guid ParseGuid(string id)
    {
        if (!Guid.TryParse(id, out var gid))
            throw new GlobalAppException("INVALID_ID_FORMAT");
        return gid;
    }

    private string EnsureNonEmpty(string? val, string error)
    {
        if (string.IsNullOrWhiteSpace(val))
            throw new GlobalAppException(error);

        return val.Trim();
    }
}