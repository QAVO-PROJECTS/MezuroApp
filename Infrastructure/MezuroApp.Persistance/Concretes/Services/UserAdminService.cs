using MezuroApp.Application.Abstracts.Repositories.NewsletterSubscribers;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.AdminUsers;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MezuroApp.Persistance.Concretes.Services;

public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<User> _userManager;
    private readonly IOrderReadRepository _orderRead;
    private readonly INewsletterSubscriberReadRepository _newsletterRead;

    private const string USER_ROLE = "Customer";

    public UserAdminService(
        UserManager<User> userManager,
        IOrderReadRepository orderRead,
        INewsletterSubscriberReadRepository newsletterRead)
    {
        _userManager = userManager;
        _orderRead = orderRead;
        _newsletterRead = newsletterRead;
    }

    // =========================
    // LIST (Registered Users)
    // =========================
    public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(AdminUsersFilterDto filter)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 10 : filter.PageSize;

        var q = _userManager.Users
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        // Search (email / name / phone)
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLowerInvariant();
            q = q.Where(x =>
                (x.Email != null && x.Email.ToLower().Contains(s)) ||
                (x.FirstName != null && x.FirstName.ToLower().Contains(s)) ||
                (x.LastName != null && x.LastName.ToLower().Contains(s)) ||
                (x.PhoneNumber != null && x.PhoneNumber.ToLower().Contains(s))
            );
        }

        // Email Confirmed filter
        if (filter.EmailConfirmed.HasValue)
            q = q.Where(x => x.EmailConfirmed == filter.EmailConfirmed.Value);

        // 1) əvvəlcə filterli user-ları oxuyuruq (page üçün)
        var rawUsers = await q
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.FirstName,
                x.LastName,
                x.PhoneNumber,
                x.EmailConfirmed,
                x.CreatedAt
            })
            .ToListAsync();

        // 2) yalnız Customer role olanları saxla (role yoxlaması)
        var customerUsers = new List<dynamic>(capacity: rawUsers.Count);

        foreach (var u in rawUsers)
        {
            var identityUser = await _userManager.FindByIdAsync(u.Id.ToString());
            if (identityUser == null) continue;

            var roles = await _userManager.GetRolesAsync(identityUser);
            if (roles.Contains(USER_ROLE))
                customerUsers.Add(u);
        }

        // 3) NewsletterSubscribed filter (istəsən burada tətbiq edək)
        // əvvəl newsletter map üçün userIds
        var customerIds = customerUsers.Select(x => (Guid)x.Id).ToList();

        var newsletterRows = await _newsletterRead.GetAllAsync(n =>
            !n.IsDeleted && n.UserId != null && customerIds.Contains(n.UserId.Value),
            enableTracking: false);

        var newsletterMap = newsletterRows
            .GroupBy(x => x.UserId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.SubscribedAt).First());

        if (filter.NewsletterSubscribed.HasValue)
        {
            var desired = filter.NewsletterSubscribed.Value;

            customerUsers = customerUsers
                .Where(u =>
                {
                    newsletterMap.TryGetValue((Guid)u.Id, out var sub);
                    var isSubscribed = sub != null && sub.IsActive;
                    return isSubscribed == desired;
                })
                .ToList();
        }

        // ✅ TotalCount artıq real (Customer + digər filterlər)
        var total = customerUsers.Count;

        // 4) paging artıq customer list-də
        var pagedUsers = customerUsers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pagedUsers.Select(u =>
        {
            newsletterMap.TryGetValue((Guid)u.Id, out var sub);

            return new AdminUserListItemDto
            {
                Id = ((Guid)u.Id).ToString(),
                Email = (string?)u.Email,
                FirstName = (string?)u.FirstName,
                LastName = (string?)u.LastName,
                PhoneNumber = (string?)u.PhoneNumber,
                EmailConfirmed = (bool)u.EmailConfirmed,
                NewsletterSubscribed = sub != null && sub.IsActive,
                CreatedAt = ((DateTime)u.CreatedAt).ToString("dd.MM.yyyy, HH:mm"),
            };
        }).ToList();

        return new PagedResult<AdminUserListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    // =========================
    // DETAIL (User Details page)
    // =========================
    public async Task<AdminUserDetailDto> GetUserDetailAsync(string userId, int ordersTake = 20)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        if (ordersTake <= 0) ordersTake = 20;
        if (ordersTake > 100) ordersTake = 100;

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == uid);

        if (user == null)
            throw new GlobalAppException("USER_NOT_FOUND");

        // ✅ yalnız customer detail açılsın
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(USER_ROLE))
            throw new GlobalAppException("USER_IS_NOT_CUSTOMER");

        var sub = await _newsletterRead.GetAsync(x => !x.IsDeleted && x.UserId == uid, enableTracking: false);

        var orders = await _orderRead.GetAllAsync(
            o => !o.IsDeleted && o.UserId == uid,
            enableTracking: false);

        var totalOrders = orders.Count;
        var totalSpent = orders.Sum(x => x.Total);
        var lastOrderDate = orders.Count == 0 ? (DateTime?)null : orders.Max(x => x.CreatedDate);

        var orderRows = orders
            .OrderByDescending(x => x.CreatedDate)
            .Take(ordersTake)
            .Select(x => new AdminUserOrderListItemDto
            {
                Id = x.Id.ToString(),
                OrderNumber = x.OrderNumber,
                Status = x.Status,
                Total = x.Total,
                OrderDate = x.CreatedDate.ToString("dd.MM.yyyy, HH:mm")
            })
            .ToList();

        return new AdminUserDetailDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,

            NewsletterSubscribed = sub != null && sub.IsActive,
            RegistrationDate = user.CreatedAt.ToString("dd.MM.yyyy, HH:mm"),

            TotalOrders = totalOrders,
            TotalSpent = totalSpent,
            LastOrderDate = lastOrderDate?.ToString("dd.MM.yyyy, HH:mm"),

            Orders = orderRows
        };
    }
}