using System.Globalization;
using System.Text;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Application.Dtos.Option;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.EntityFrameworkCore;

public class OptionService : IOptionService
{
    private readonly IOptionReadRepository _readRepo;
    private readonly IOptionWriteRepository _writeRepo;
    private readonly IMapper _mapper;

    public OptionService(
        IOptionReadRepository readRepo,
        IOptionWriteRepository writeRepo,
        IMapper mapper)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _mapper = mapper;
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
    }

    public async Task UpdateAsync(UpdateOptionDto dto)
    {
        var gid = ParseGuid(dto.Id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("OPTION_NOT_FOUND");

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

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;

        await _writeRepo.CommitAsync();
    }

    // Helpers
    private Guid ParseGuid(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new GlobalAppException("INVALID_ID_FORMAT");

        return guid;
    }
}