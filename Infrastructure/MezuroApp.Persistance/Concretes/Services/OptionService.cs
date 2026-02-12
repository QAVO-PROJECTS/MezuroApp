using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Application.Dtos.Option;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;

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