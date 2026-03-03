namespace MezuroApp.Application.Abstracts.Services;

using MezuroApp.Domain.HelperEntities;
using MezuroApp.Application.Dtos.AdminUsers;

public interface IUserAdminService
{
    Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(AdminUsersFilterDto filter);
    Task<AdminUserDetailDto> GetUserDetailAsync(string userId, int ordersTake = 20);
}