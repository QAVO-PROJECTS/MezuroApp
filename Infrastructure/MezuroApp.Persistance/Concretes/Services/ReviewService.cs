using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.Reviews;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Review;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace MezuroApp.Persistance.Concretes.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewReadRepository _readRepo;
    private readonly IReviewWriteRepository _writeRepo;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IAuditHelper _audit;

    public ReviewService(IReviewReadRepository readRepo, IReviewWriteRepository writeRepo, IMapper mapper, UserManager<User> userManager, IAuditHelper audit)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _mapper = mapper;
        _userManager = userManager;
        _audit = audit;
        
        
    }

    public async Task<ReviewDto> GetByIdAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted && x.Status==true,
            q => q.Include(r => r.User).Include(q=>q.Product)
                .ThenInclude(p=>p.Images),
            enableTracking: false
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");

        return _mapper.Map<ReviewDto>(entity);
    }

  

    public async Task<PagedResult<ReviewDto>> GetAllByProductAsync(string productId, int page = 1, int pageSize = 10)
    {
        var pid = ParseGuidOrThrow(productId);

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == true && x.ProductId == pid);

        var totalCount = await query.CountAsync();

        var entities = await query
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(entities),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllInActiveAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == false);

        var totalCount = await query.CountAsync();

        var entities = await query
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(entities),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllForAdminAsync(int page = 1, int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking().Where(x => x.Status == true || (x.Status == false && x.IsDeleted == true));
           

        var totalCount = await query.CountAsync();

        var entities = await query
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(entities),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllInactiveByProductForAdminAsync(string productId, int page = 1, int pageSize = 10)
    {
        var pid = ParseGuidOrThrow(productId);

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == false && x.ProductId == pid);

        var totalCount = await query.CountAsync();

        var entities = await query
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(entities),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllActiveByProductForAdminAsync(string productId, int page = 1, int pageSize = 10)
    {
        var pid = ParseGuidOrThrow(productId);

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking()
            .Where(x => x.ProductId == pid && x.Status == true);

        var totalCount = await query.CountAsync();

        var entities = await query
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(entities),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllInactiveByRatingForAdminAsync(int rating, int page = 1, int pageSize = 10)
    {
     

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == false && x.Rating == rating);

        var totalCount = await query.CountAsync();

        var entities = await query
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(entities),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllActiveByRatingForAdminAsync(int rating, int page = 1, int pageSize = 10)
    {
     

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking()
            .Where(x => x.Rating == rating  && x.Status == true);

        var totalCount = await query.CountAsync();

        var entities = await query
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(entities),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllInActiveSortedAsync(
        int sort = 2,
        int page = 1,
        int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        IQueryable<Review> q = _readRepo.Query()
            .AsNoTracking()
            .Where(r => !r.IsDeleted && r.Status == false);

        // Sort
        q = sort switch
        {
            1 => q.OrderBy(r => r.CreatedDate),            // köhnə -> yeni
            2 => q.OrderByDescending(r => r.CreatedDate),  // yeni -> köhnə
            3 => q.OrderBy(r => r.Rating),                 // az -> çox
            4 => q.OrderByDescending(r => r.Rating),       // çox -> az
            _ => q.OrderByDescending(r => r.CreatedDate)
        };

        var total = await q.CountAsync();

        // Include-ni sonda et
        var items = await q
            .Include(r => r.User).Include(x=>x.Product)
            .ThenInclude(p=>p.Images)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(items),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
    public async Task<PagedResult<ReviewDto>> GetAllActiveSortedAsync(
        int sort = 2,
        int page = 1,
        int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        IQueryable<Review> q = _readRepo.Query()
            .AsNoTracking()
            .Where(r => r.Status == true);
            ;

        // Sort
        q = sort switch
        {
            1 => q.OrderBy(r => r.CreatedDate),            // köhnə -> yeni
            2 => q.OrderByDescending(r => r.CreatedDate),  // yeni -> köhnə
            3 => q.OrderBy(r => r.Rating),                 // az -> çox
            4 => q.OrderByDescending(r => r.Rating),       // çox -> az
            _ => q.OrderByDescending(r => r.CreatedDate)
        };

        var total = await q.CountAsync();

        // Include-ni sonda et
        var items = await q
            .Include(r => r.User).Include(x=>x.Product)
            .ThenInclude(p=>p.Images)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(items),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
    
    public async Task<PagedResult<ReviewDto>> GetByStatusAndDeleteAsync(
        bool value,
        int page = 1,
        int pageSize = 10)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _readRepo.Query()
            .AsNoTracking()
            .Include(r => r.User).Include(q=>q.Product)
            .ThenInclude(p=>p.Images)
            .Where(r => r.IsDeleted == value && r.Status == value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewDto>
        {
            Items = _mapper.Map<List<ReviewDto>>(items),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }



    public async Task InCreaseAsync(string reviewId) // like +
    {
        var rid = ParseGuidOrThrow(reviewId);

        var entity = await _readRepo.GetAsync(
            x => x.Id == rid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");

        entity.LikeCount = (entity.LikeCount ?? 0) + 1;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }

    public async Task DeCrease(string reviewId) // dislike +
    {
        var rid = ParseGuidOrThrow(reviewId);

        var entity = await _readRepo.GetAsync(
            x => x.Id == rid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");

        entity.DislikeCount = (entity.DislikeCount ?? 0) + 1;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }

    // İnterfeys imzasına uyğun (ReviewDto qəbul edir)


    // Praktik overlod — Controller bunu çağıracaq (UserId lazım olduğu üçün)

    public async Task CreateAsync(CreateReviewDto dto)
    {
        if (dto is null) throw new GlobalAppException("INVALID_INPUT");
        ParseGuidOrThrow(dto.ProductId);

        // 1) Map-lə başlanğıc entity
        var entity = _mapper.Map<Review>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedDate = DateTime.UtcNow;
        entity.LastUpdatedDate = entity.CreatedDate;
        entity.IsDeleted = false;
        entity.Status = false;

        // 2) UserId normalizasiya
        Guid? normalizedUserId = null;

        if (!string.IsNullOrWhiteSpace(dto.UserId))
        {
            var text = dto.UserId.Trim();

            // "null" göndərilə bilirsə, onu boş say
            if (!string.Equals(text, "null", StringComparison.OrdinalIgnoreCase))
            {
                if (Guid.TryParse(text, out var parsed) && parsed != Guid.Empty)
                {
                    // user mövcuddurmu?
                    var user = await _userManager.FindByIdAsync(parsed.ToString());
                    if (user != null)
                        normalizedUserId = parsed; // yalnız mövcuddursa yaz
                }
            }
        }

        entity.UserId = normalizedUserId; // mövcud deyilsə null

        await _writeRepo.AddAsync(entity);
        await _writeRepo.CommitAsync();
    }

    public async Task ReplyAsync(ReplyReviewDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.ReviewId))
            throw new GlobalAppException("INVALID_INPUT");

        var rid = ParseGuidOrThrow(dto.ReviewId);

        var entity = await _readRepo.GetAsync(
            x => x.Id == rid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");
        var oldValues = new Dictionary<string, object>
        {
            ["AdminReplyDescription"] = entity.AdminReplyDescription ?? "",
            ["AdminReplyDate"] = entity.AdminReplyDate?.ToString("dd.MM.yyyy HH:mm:ss") ?? ""
        };

        entity.AdminReplyDescription = dto.Description;
        entity.AdminReplyDate = DateTime.UtcNow.AddHours(4);
        entity.LastUpdatedDate = DateTime.UtcNow.AddHours(4);

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
        await _audit.LogAsync(
            "Reviews",
            "UPDATE",
            "REVIEW_REPLIED",
            entity.Id,
            oldValues,
            new Dictionary<string, object>
            {
                ["AdminReplyDescription"] = entity.AdminReplyDescription ?? "",
                ["AdminReplyDate"] = entity.AdminReplyDate?.ToString("dd.MM.yyyy HH:mm:ss") ?? ""
            }
        );
    }

    public async Task  RejectAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");
        var oldValues = new Dictionary<string, object>
        {
        
            ["Status"] = entity.Status ?? false,
            ["Rating"] = entity.Rating ?? 0,
            ["Description"] = entity.Description ?? ""
        };

        entity.IsDeleted = true;

        entity.DeletedDate = DateTime.UtcNow;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
        await _audit.LogAsync(
            "Reviews",
            "DELETE",
            "REVIEW_REJECTED",
            entity.Id,
            oldValues,
            new Dictionary<string, object>
            {
             
                ["DeletedDate"] = entity.DeletedDate.ToString("dd.MM.yyyy HH:mm:ss")
            }
        );
    }

    public async Task EditStatusAsync(string id, bool status)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");
        var oldValues = new Dictionary<string, object>
        {
            ["Status"] = entity.Status ?? false
        };

        entity.Status = status;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
        await _audit.LogAsync(
            "Reviews",
            "UPDATE",
            status ? "REVIEW_APPROVED" : "REVIEW_UNAPPROVED",
            entity.Id,
            oldValues,
            new Dictionary<string, object>
            {
                ["Status"] = entity.Status ?? false
            }
        );
    }


    public async Task<List<ReviewDto>> SortReview(SortReviewDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.ProductId))
            throw new GlobalAppException("INVALID_INPUT");

        var pid = ParseGuidOrThrow(dto.ProductId);

        // Repo list qaytarır → include User
        var list = await _readRepo.GetAllAsync(
            x => x.ProductId == pid && !x.IsDeleted && x.Status==true,
            q => q.Include(r => r.User).Include(x=>x.Product)
        );

        IEnumerable<Review> ordered;

        if (dto.SortNewAndOld.HasValue)
        {
            ordered = dto.SortNewAndOld.Value
                ? list.OrderByDescending(r => r.CreatedDate) // Newest
                : list.OrderBy(r => r.CreatedDate);          // Oldest
        }
        else if (dto.SortLikeAndDislike.HasValue)
        {
            // Most/Least helpful
            ordered = dto.SortLikeAndDislike.Value
                ? list.OrderByDescending(r => r.LikeCount ?? 0)
                : list.OrderBy(r => r.LikeCount ?? 0);
        }
        else if (dto.SortRating.HasValue)
        {
            // Highest/Lowest rating
            ordered = dto.SortRating.Value
                ? list.OrderByDescending(r => r.Rating ?? 0)
                : list.OrderBy(r => r.Rating ?? 0);
        }
        else
        {
            // Default: Newest
            ordered = list.OrderByDescending(r => r.CreatedDate);
        }

        var finalList = ordered.ToList();
        return _mapper.Map<List<ReviewDto>>(finalList);
    }


    // Helpers
    private static Guid ParseGuidOrThrow(string id)
    {
        if (!Guid.TryParse(id, out var gid))
            throw new GlobalAppException("INVALID_ID_FORMAT");
        return gid;
    }
}
