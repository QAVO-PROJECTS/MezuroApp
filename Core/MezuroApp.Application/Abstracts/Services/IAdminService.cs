using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Application.Abstracts.Services
{
    public interface IAdminService
    {
        // SuperAdmin → Admin yaratmaq
        Task<AdminCreateResponseDto> CreateAsync(AdminCreateRequestDto dto, Guid actorId, bool superAdmin);

        // Admin / SuperAdmin portal login
        Task<AdminLoginResponseDto> LoginAsync(AdminLoginRequestDto dto);

        // Admin öz şifrəsini dəyişir
        Task ChangePasswordAsync(Guid adminId, AdminChangePasswordDto dto);

        // SuperAdmin və ya Admin e-mail reset link
        Task SendResetPasswordLinkAsync(AdminResetPasswordRequestDto dto);

        // İstifadəçi linkdən girib şifrəni təyin edir
        Task ResetPasswordAsync(AdminResetPasswordConfirmDto dto);

        // Admin profil update
        Task UpdateProfileAsync(Guid adminId, AdminUpdateProfileDto dto);

        // Permission idarəsi (SuperAdmin)
        Task SetPermissionsAsync( SetPermissionsDto dto, Guid actorId);
        Task AddRemovePermissionsAsync(AddRemovePermissionsDto dto, Guid actorId);

        // Baxış
        Task<AdminDto> GetByIdAsync(Guid adminId);
        Task<AdminDto> GetByEmailAsync(string email);
        Task<AdminUpdateResponseDto> UpdateAdminAsync(AdminUpdateRequestDto dto, Guid actorId);
 
        Task<IEnumerable<string>> GetPermissionsAsync(Guid adminId);
        Task<PagedResult<AdminDto>> GetAllAdminsAsync(string id,bool? isActive, int page = 1, int pageSize = 10);
        Task SetAdminActiveAsync(string adminId, bool value, Guid actorId);
        Task DeleteOrRevokeAdminAsync(string adminId, Guid actorId);
    }
}