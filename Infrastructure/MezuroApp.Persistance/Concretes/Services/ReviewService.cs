using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MezuroApp.Application.Abstracts.Repositories.Reviews;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Review;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace MezuroApp.Persistance.Concretes.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewReadRepository _readRepo;
    private readonly IReviewWriteRepository _writeRepo;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;

    public ReviewService(IReviewReadRepository readRepo, IReviewWriteRepository writeRepo, IMapper mapper, UserManager<User> userManager)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _mapper = mapper;
        _userManager = userManager;
        
    }

    public async Task<ReviewDto> GetByIdAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            q => q.Include(r => r.User),
            enableTracking: false
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");

        return _mapper.Map<ReviewDto>(entity);
    }

    public async Task<List<ReviewDto>> GetAllAsync(string productId)
    {
        var pid = ParseGuidOrThrow(productId);

        var entities = await _readRepo.GetAllAsync(
            x => x.ProductId == pid && !x.IsDeleted,
            q => q.Include(r => r.User)
                  .OrderByDescending(r => r.CreatedDate)
        );

        return _mapper.Map<List<ReviewDto>>(entities);
    }

    public async Task<List<ReviewDto>> GetAllActiveAsync(string productId)
    {
        var pid = ParseGuidOrThrow(productId);

        var entities = await _readRepo.GetAllAsync(
            x => x.ProductId == pid && x.Status == true && !x.IsDeleted,
            q => q.Include(r => r.User)
                  .OrderByDescending(r => r.CreatedDate)
        );

        return _mapper.Map<List<ReviewDto>>(entities);
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

        entity.AdminReplyDescription = dto.Description;
        entity.AdminReplyDate = DateTime.UtcNow;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");

        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }

    public async Task EditStatusAsync(string id, bool status)
    {
        var gid = ParseGuidOrThrow(id);

        var entity = await _readRepo.GetAsync(
            x => x.Id == gid && !x.IsDeleted,
            enableTracking: true
        ) ?? throw new GlobalAppException("REVIEW_NOT_FOUND");

        entity.Status = status;
        entity.LastUpdatedDate = DateTime.UtcNow;

        await _writeRepo.UpdateAsync(entity);
        await _writeRepo.CommitAsync();
    }


    public async Task<List<ReviewDto>> SortReview(SortReviewDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.ProductId))
            throw new GlobalAppException("INVALID_INPUT");

        var pid = ParseGuidOrThrow(dto.ProductId);

        // Repo list qaytarır → include User
        var list = await _readRepo.GetAllAsync(
            x => x.ProductId == pid && !x.IsDeleted && x.Status==true,
            q => q.Include(r => r.User)
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
