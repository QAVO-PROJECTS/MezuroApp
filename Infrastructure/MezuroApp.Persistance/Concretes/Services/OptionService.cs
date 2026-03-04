using System.Globalization;
using System.Text;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Application.Dtos.Option;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MezuroApp.Application.Abstracts.Services;

public class OptionService : IOptionService
{
    private readonly IOptionReadRepository _readRepo;
    private readonly IOptionWriteRepository _writeRepo;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _audit;
    private readonly IHttpContextAccessor _http;

    public OptionService(
        IOptionReadRepository readRepo,
        IOptionWriteRepository writeRepo,
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

    public async Task<OptionDto> GetByIdAsync(string id)
    {
        var gid = ParseGuid(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted
        ) ?? throw new GlobalAppException("OPTION_NOT_FOUND");

        return _mapper.Map<OptionDto>(entity);
    }

    public async Task<List<OptionDto>> GetAllAsync()
    {
        var list = await _readRepo.GetAllAsync(x => !x.IsDeleted);

        return _mapper.Map<List<OptionDto>>(list);
    }

    public async Task CreateAsync(CreateOptionDto dto)
    {
        // Unikallıq — NameAz əsas götürülür
        var nameExists = await _readRepo.GetAsync(
            x => !x.IsDeleted && x.NameAz.ToLower() == dto.NameAz.ToLower()
        );

        if (nameExists != null)
            throw new GlobalAppException("OPTION_NAME_ALREADY_EXISTS");

        var entity = _mapper.Map<Option>(dto);

        entity.Id = Guid.NewGuid();
        entity.CreatedDate = DateTime.UtcNow;
        entity.LastUpdatedDate = entity.CreatedDate;
        entity.IsDeleted = false;

        await _writeRepo.AddAsync(entity);
        await _writeRepo.CommitAsync();
        await WriteAuditAsync(
            action: "CREATE",
            entityId: entity.Id,
            oldValues: null,
            newValues: OptionSnap(entity)
        );
    }

    public async Task UpdateAsync(UpdateOptionDto dto)
    {
        var gid = ParseGuid(dto.Id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("OPTION_NOT_FOUND");

        var oldSnap = OptionSnap(entity);
        // Unikallıq yoxlaması (Name dəyişibsə)
        if (!string.IsNullOrWhiteSpace(dto.NameAz))
        {
            var exists = await _readRepo.GetAsync(
                x => x.Id != gid &&
                     !x.IsDeleted &&
                     x.NameAz.ToLower() == dto.NameAz.ToLower()
            );

            if (exists != null)
                throw new GlobalAppException("OPTION_NAME_ALREADY_EXISTS");
        }

        _mapper.Map(dto, entity);
        entity.LastUpdatedDate = DateTime.UtcNow;
        await _writeRepo.CommitAsync();

        await WriteAuditAsync(
            action: "UPDATE",
            entityId: entity.Id,
            oldValues: oldSnap,
            newValues: OptionSnap(entity)
        );
    }
    public async Task<PagedResult<OptionDto>> SearchAsync(string? query, int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 20;

        var normalizedQ = NormalizeSearch(query);

        var baseList = await _readRepo.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new
            {
                Entity = x,
                NameAz = x.NameAz,
                NameEn = x.NameEn,
                NameRu = x.NameRu,
                NameTr = x.NameTr
            })
            .ToListAsync();

        var filtered = string.IsNullOrWhiteSpace(normalizedQ)
            ? baseList
            : baseList.Where(x =>
                NormalizeSearch(x.NameAz).Contains(normalizedQ) ||
                NormalizeSearch(x.NameEn).Contains(normalizedQ) ||
                NormalizeSearch(x.NameRu).Contains(normalizedQ) ||
                NormalizeSearch(x.NameTr).Contains(normalizedQ)
            ).ToList();

        var total = filtered.Count;

        var items = filtered
            .OrderByDescending(x => x.Entity.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Entity)
            .ToList();

        return new PagedResult<OptionDto>
        {
            Items = _mapper.Map<List<OptionDto>>(items),
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

    public async Task DeleteAsync(string id)
    {
        var gid = ParseGuid(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("OPTION_NOT_FOUND");
        var oldSnap = OptionSnap(entity);

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;

        await _writeRepo.CommitAsync();

        await WriteAuditAsync(
            action: "DELETE",
            entityId: entity.Id,
            oldValues: oldSnap,
            newValues: OptionSnap(entity)
        );
    }

    // Helpers
    private Guid ParseGuid(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new GlobalAppException("INVALID_ID_FORMAT");

        return guid;
    }
    private bool IsAdminRequest()
    {
        var user = _http.HttpContext?.User;
        if (user == null) return false;

        if (user.IsInRole("SuperAdmin") || user.IsInRole("Admin") || user.IsInRole("Owner"))
            return true;

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

    private static Dictionary<string, object> OptionSnap(Option o) => new()
    {
        ["id"] = o.Id.ToString(),
        ["nameAz"] = o.NameAz,
        ["nameEn"] = o.NameEn,
        ["nameRu"] = o.NameRu,
        ["nameTr"] = o.NameTr,
        ["createdDate"] = o.CreatedDate,
        ["lastUpdatedDate"] = o.LastUpdatedDate,
        ["isDeleted"] = o.IsDeleted
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
            Module = "Options",
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