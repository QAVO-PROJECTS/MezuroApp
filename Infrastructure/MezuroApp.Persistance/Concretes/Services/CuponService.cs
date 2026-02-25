using System.Globalization;
using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.Cupons;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Cupon;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MezuroApp.Persistance.Concretes.Services
{
    public class CuponService : ICuponService
    {
        private readonly ICuponReadRepository _readRepo;
        private readonly ICuponWriteRepository _writeRepo;
        private readonly IMapper _mapper;

        public CuponService(
            ICuponReadRepository readRepo,
            ICuponWriteRepository writeRepo,
            IMapper mapper)
        {
            _readRepo = readRepo;
            _writeRepo = writeRepo;
            _mapper = mapper;
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
        }

  public async Task UpdateCupon(UpdateCuponDto dto)
{
    var gid = ParseGuidOrThrow(dto.Id);

    var entity = await _readRepo.GetAsync(
        x => x.Id == gid && !x.IsDeleted,
        q => q.Include(x => x.Admin),
        enableTracking: true
    ) ?? throw new GlobalAppException("NOT_FOUND_CUPON");

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
        entity.DiscountType = dto.DiscountType == DiscountType.Percentage.ToString()
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
}



        public async Task DeleteCupon(string cuponId)
        {
            var gid = ParseGuidOrThrow(cuponId);

            var entity = await _readRepo.GetAsync(
                x => x.Id == gid && !x.IsDeleted
            ) ?? throw new GlobalAppException("NOT_FOUND_CUPON");

            entity.IsDeleted = true;
            entity.DeletedDate = DateTime.UtcNow;

            await _writeRepo.UpdateAsync(entity);
            await _writeRepo.CommitAsync();
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
            cupon.IsActive =value;
            cupon.LastUpdatedDate = DateTime.UtcNow;
            await _writeRepo.UpdateAsync(cupon);
    
            await _writeRepo.CommitAsync();
            
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
    }
}
