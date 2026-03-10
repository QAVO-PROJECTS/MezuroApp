using System.Globalization;
using System.Text;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Cupons;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Cupon;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace MezuroApp.Persistance.Concretes.Services
{
    public class CuponService : ICuponService
    {
        private readonly ICuponReadRepository _readRepo;
        private readonly ICuponWriteRepository _writeRepo;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _audit;
        private readonly IHttpContextAccessor _http;
        

        public CuponService(
            ICuponReadRepository readRepo,
            ICuponWriteRepository writeRepo,
            IMapper mapper,
            IAuditLogService audit,
            IHttpContextAccessor http)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
            _mapper = mapper;
            _audit = audit;
            _http = http;
        }

        // ====================================================================
        //                            QUERIES
        // ====================================================================

        public async Task<List<CuponDto>> GetAllCupons()
        {
            var cupons = await _readRepo.GetAllAsync(
                x => !x.IsDeleted,
                q => q.Include(x => x.Admin)
            );

            return _mapper.Map<List<CuponDto>>(cupons);
        }

        public async Task<List<CuponDto>> GetAllFilterCupons(
            string? validFrom,
            string? validUntil,
            bool? isActive,
            int pageNumber,
            int pageSize)
        {
            var skip = (pageNumber - 1) * pageSize;

            DateTime? fromDate = null;
            DateTime? untilDate = null;

            // dd.MM.yyyy parse
            if (!string.IsNullOrWhiteSpace(validFrom))
            {
                if (DateTime.TryParseExact(
                        validFrom,
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedFrom))
                {
                    fromDate = parsedFrom.Date;
                }
                else
                {
                    throw new GlobalAppException("INVALID_VALID_FROM_DATE_FORMAT");
                }
            }

            if (!string.IsNullOrWhiteSpace(validUntil))
            {
                if (DateTime.TryParseExact(
                        validUntil,
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedUntil))
                {
                    // günün sonuna qədər
                    untilDate = parsedUntil.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    throw new GlobalAppException("INVALID_VALID_UNTIL_DATE_FORMAT");
                }
            }

            var query = _readRepo.Query()
                .Where(x => !x.IsDeleted);

            // IsActive filter (null deyilsə)
            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            // ValidFrom filter
            if (fromDate.HasValue)
                query = query.Where(x => x.ValidFrom >= fromDate.Value);

            // ValidUntil filter
            if (untilDate.HasValue)
                query = query.Where(x => x.ValidUntil <= untilDate.Value);

            var cupons = await query
                .Include(x => x.Admin)
                .OrderByDescending(x => x.CreatedDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<List<CuponDto>>(cupons);
        }

      public async Task<PagedResult<CuponDto>> SearchByCodeAsync(string? query, int pageNumber, int pageSize)
{
    if (pageNumber <= 0) pageNumber = 1;
    if (pageSize <= 0) pageSize = 20;

    var normalizedQ = NormalizeSearch(query);

    // DB-dən minimal götür (performans üçün)
    var baseList = await _readRepo.Query()
        .AsNoTracking()
        .Where(x => !x.IsDeleted)
        .Include(x => x.Admin)
        .Select(x => new
        {
            Entity = x,
            Code = x.Code
        })
        .ToListAsync();

    // Memory-də fuzzy filter
    var filtered = string.IsNullOrWhiteSpace(normalizedQ)
        ? baseList
        : baseList.Where(x => NormalizeSearch(x.Code).Contains(normalizedQ)).ToList();

    var total = filtered.Count;

    var items = filtered
        .OrderByDescending(x => x.Entity.CreatedDate)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Select(x => x.Entity)
        .ToList();

    return new PagedResult<CuponDto>
    {
        Items = _mapper.Map<List<CuponDto>>(items),
        Page = pageNumber,
        PageSize = pageSize,
        TotalCount = total
    };
}

private static string NormalizeSearch(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return "";

    input = input.Trim().ToLowerInvariant();

    // diacritics remove (ə,ö,ü,ğ,ç,ş və s.)
    var normalized = input.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();
    foreach (var c in normalized)
    {
        var uc = CharUnicodeInfo.GetUnicodeCategory(c);
        if (uc != UnicodeCategory.NonSpacingMark)
            sb.Append(c);
    }

    var res = sb.ToString().Normalize(NormalizationForm.FormC);

    // AZ xüsusi hərfləri əlavə xəritələ (ə -> e kimi istəsən)
    res = res
        .Replace('ə', 'e')
        .Replace('ı', 'i')
        .Replace('ö', 'o')
        .Replace('ü', 'u')
        .Replace('ğ', 'g')
        .Replace('ş', 's')
        .Replace('ç', 'c');

    // boşluq/tire/altxətt standart
    res = res.Replace("-", " ").Replace("_", " ");
    while (res.Contains("  ")) res = res.Replace("  ", " ");

    return res;
}

        public async Task<List<CuponDto>> PagedGetAllCupons(int pageNumber, int pageSize)
        {
            var skip = (pageNumber - 1) * pageSize;

            var cupons = await _readRepo.GetPagedAsync(x => !x.IsDeleted,
                q => q.Include(x => x.Admin)
                    .Skip(skip).Take(pageSize));
   

            return _mapper.Map<List<CuponDto>>(cupons);
        }

        public async Task<List<CuponDto>> PagedGetAllActiveCupons(int pageNumber, int pageSize)
        {
            var skip = (pageNumber - 1) * pageSize;

            var cupons = await _readRepo.GetPagedAsync(x => !x.IsDeleted && x.IsActive,
                q => q.Include(x => x.Admin)
                    .Skip(skip).Take(pageSize));

            return _mapper.Map<List<CuponDto>>(cupons);
        }

        public async Task<List<CuponDto>> PagedGetAllInactiveCupons(int pageNumber, int pageSize)
        {
            var skip = (pageNumber - 1) * pageSize;

            var cupons = await _readRepo.GetPagedAsync(x => !x.IsDeleted && !x.IsActive,
                q => q.Include(x => x.Admin)
                    .Skip(skip).Take(pageSize));

            return _mapper.Map<List<CuponDto>>(cupons);
        }

        public async Task<CuponDto?> GetCuponById(string cuponId)
        {
            var gid = ParseGuidOrThrow(cuponId);

            var cupon = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted,
                q => q.Include(x => x.Admin)
            );

            return cupon == null ? null : _mapper.Map<CuponDto>(cupon);
        }

        public async Task<CuponDto?> GetCuponByCode(string cuponCode)
        {
            if (string.IsNullOrWhiteSpace(cuponCode))
                throw new GlobalAppException("CUPON_CODE_REQUIRED");

            var cupon = await _readRepo.GetAsync(
                x => x.Code.ToLower() == cuponCode.ToLower() && !x.IsDeleted,
                q => q.Include(x => x.Admin)
            );

            return cupon == null ? null : _mapper.Map<CuponDto>(cupon);
        }

        // ====================================================================
        //                            COMMANDS
        // ====================================================================

    

   

        public async Task CreateCupon(string adminId, CreateCuponDto dto)
        {
            var existing = await _readRepo.GetAsync(
                x => !x.IsDeleted && x.Code.ToLower() == dto.Code.ToLower()
            );

            if (existing != null)
                throw new GlobalAppException("UNIQE_CUPON");

            var entity = _mapper.Map<Cupon>(dto);

            entity.Id = Guid.NewGuid();
            entity.CreatedDate = DateTime.UtcNow;
            entity.LastUpdatedDate = entity.CreatedDate;
            entity.IsDeleted = false;
            entity.AdminId = Guid.Parse(adminId);
            
      

            entity.DiscountType = dto.DiscountType == DiscountType.Percentage
                ? "percentage"
                : "fixed_amount";


            const string dateFormat = "dd.MM.yyyy HH:mm";

            // VALID FROM
            if (!string.IsNullOrWhiteSpace(dto.ValidFrom))
            {
                if (!DateTime.TryParseExact(dto.ValidFrom,
                        dateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedFrom))
                    throw new GlobalAppException("INVALID_DATE_FORMAT");

                // MUST – PostgreSQL timestamptz only accepts UTC
                entity.ValidFrom = DateTime.SpecifyKind(parsedFrom.AddHours(-4), DateTimeKind.Utc);
            }

            // VALID UNTIL
            if (!string.IsNullOrWhiteSpace(dto.ValidUntil))
            {
                if (!DateTime.TryParseExact(dto.ValidUntil,
                        dateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedUntil))
                    throw new GlobalAppException("INVALID_DATE_FORMAT");

                entity.ValidUntil = DateTime.SpecifyKind(parsedUntil.AddHours(-4), DateTimeKind.Utc);
            }

            await _writeRepo.AddAsync(entity);
            await _writeRepo.CommitAsync();
            await WriteAuditAsync(
                action: "CREATE",
                entityId: entity.Id,
                oldValues: null,
                newValues: CuponSnap(entity)
            );
        }

  public async Task UpdateCupon(UpdateCuponDto dto)
{
    var gid = ParseGuidOrThrow(dto.Id);

    var entity = await _readRepo.GetAsync(
        x => x.Id == gid && !x.IsDeleted,
        q => q.Include(x => x.Admin),
        enableTracking: true
    ) ?? throw new GlobalAppException("NOT_FOUND_CUPON");
    var oldSnap = CuponSnap(entity);

    // Code unique check (yalnız Code göndərilibsə)
    if (!string.IsNullOrWhiteSpace(dto.Code))
    {
        var codeExists = await _readRepo.GetAsync(
            x => !x.IsDeleted && x.Code.ToLower() == dto.Code!.ToLower()
        );

        if (codeExists != null && codeExists.Id != entity.Id)
            throw new GlobalAppException("UNIQE_CUPON");

        entity.Code = dto.Code.Trim();
    }

    // Null gələnlər toxunulmur
    if (dto.DiscountType != null)
        entity.DiscountType = dto.DiscountType == DiscountType.Percentage
            ? "percentage"
            : "fixed_amount";

    if (dto.DiscountValue.HasValue)
        entity.DiscountValue = dto.DiscountValue.Value;

    if (dto.MinimumPurchaseAmount.HasValue)
        entity.MinimumPurchaseAmount = dto.MinimumPurchaseAmount;

    if (dto.MaxUses.HasValue)
        entity.MaxUses = dto.MaxUses;

    if (dto.MaxUsesPerUser.HasValue)
        entity.MaxUsesPerUser = dto.MaxUsesPerUser.Value;

    if (dto.IsActive.HasValue)
        entity.IsActive = dto.IsActive.Value;

    const string dateFormat = "dd.MM.yyyy HH:mm";

    if (!string.IsNullOrWhiteSpace(dto.ValidFrom))
    {
        if (!DateTime.TryParseExact(dto.ValidFrom, dateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedFrom))
            throw new GlobalAppException("INVALID_DATE_FORMAT");

        entity.ValidFrom = DateTime.SpecifyKind(parsedFrom.AddHours(-4), DateTimeKind.Utc);
    }

    if (!string.IsNullOrWhiteSpace(dto.ValidUntil))
    {
        if (!DateTime.TryParseExact(dto.ValidUntil, dateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedUntil))
            throw new GlobalAppException("INVALID_DATE_FORMAT");

        entity.ValidUntil = DateTime.SpecifyKind(parsedUntil.AddHours(-4), DateTimeKind.Utc);
    }

    entity.LastUpdatedDate = DateTime.UtcNow;

    await _writeRepo.UpdateAsync(entity);
    await _writeRepo.CommitAsync();
 

    await WriteAuditAsync(
        action: "UPDATE",
        entityId: entity.Id,
        oldValues: oldSnap,
        newValues: CuponSnap(entity)
    );
}



        public async Task DeleteCupon(string cuponId)
        {
            var gid = ParseGuidOrThrow(cuponId);

            var entity = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted
            ) ?? throw new GlobalAppException("NOT_FOUND_CUPON");
            var oldSnap = CuponSnap(entity);

            entity.IsDeleted = true;
            entity.DeletedDate = DateTime.UtcNow;

            await _writeRepo.UpdateAsync(entity);
            await _writeRepo.CommitAsync();

            await WriteAuditAsync(
                action: "DELETE",
                entityId: entity.Id,
                oldValues: oldSnap,
                newValues: CuponSnap(entity)
            );
        }

        public async Task SetActiveCupon(string cuponId, bool value)
        {
            var gid = ParseGuidOrThrow(cuponId);

            var cupon = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted
                
             
            );
            if (cupon == null)
            {
                throw new GlobalAppException("NOT_FOUND_CUPON");
            }
            var oldSnap = CuponSnap(cupon);
            cupon.IsActive =value;
            cupon.LastUpdatedDate = DateTime.UtcNow;
            await _writeRepo.UpdateAsync(cupon);
            await _writeRepo.CommitAsync();

            await WriteAuditAsync(
                action: "UPDATE",
                entityId: cupon.Id,
                oldValues: oldSnap,
                newValues: CuponSnap(cupon)
            );
            
        }

        // ====================================================================
        //                            HELPERS
        // ====================================================================

        private static Guid ParseGuidOrThrow(string id)
        {
            if (!Guid.TryParse(id, out var gid))
                throw new GlobalAppException("INVALID_CUPON_ID");
            return gid;
        }
        private bool IsAdminRequest()
        {
            var user = _http.HttpContext?.User;
            if (user == null) return false;

            // role-larını öz sisteminə görə
            if (user.IsInRole("SuperAdmin") || user.IsInRole("Owner") || user.IsInRole("Admin"))
                return true;

            // permission claim varsa (səndə var)
            return user.Claims.Any(c => c.Type == Permissions.ClaimType);
        }

        private string GetUserId()
        {
            var user = _http.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? user?.FindFirst("sub")?.Value
                   ?? "Anonymous";
        }

        private (string ip, string ua) GetReqInfo()
        {
            var ctx = _http.HttpContext;
            var ip = ctx?.Connection.RemoteIpAddress?.ToString() ?? "";
            var ua = ctx?.Request.Headers["User-Agent"].ToString() ?? "";
            return (ip, ua);
        }
        private static Dictionary<string, object?> CuponSnap(Cupon c) => new()
        {
            ["id"] = c.Id.ToString(),
            ["code"] = c.Code,
            ["discountType"] = c.DiscountType,
            ["discountValue"] = c.DiscountValue,
            ["minimumPurchaseAmount"] = c.MinimumPurchaseAmount,
            ["maxUses"] = c.MaxUses,
            ["maxUsesPerUser"] = c.MaxUsesPerUser,
            ["validFrom"] = c.ValidFrom?.ToString("O"),
            ["validUntil"] = c.ValidUntil?.ToString("O"),
            ["isActive"] = c.IsActive,
            ["isDeleted"] = c.IsDeleted,
            ["adminId"] = c.AdminId.ToString()
        };

        private async Task WriteAuditAsync(
            string action, // "CREATE" | "UPDATE" | "DELETE"
            Guid? entityId,
            Dictionary<string, object>? oldValues,
            Dictionary<string, object>? newValues)
        {
            if (!IsAdminRequest()) return;

            var (ip, ua) = GetReqInfo();

            await _audit.LogAsync(new AuditLog
            {
                UserId = GetUserId(),
                Module = "Coupons",
                EntityId = entityId,
                ActionType = action,
                OldValuesJson = oldValues ?? new Dictionary<string, object>(),
                NewValuesJson = newValues ?? new Dictionary<string, object>(),
                IpAddress = ip,
                UserAgent = ua,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
