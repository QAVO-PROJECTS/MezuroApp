using MezuroApp.Application.Dtos.Admins;

namespace MezuroApp.Application.Abstracts.Services
{
    public interface IAdminService
    {
        // SuperAdmin → Admin yaratmaq
        Task<AdminCreateResponseDto> CreateAsync(AdminCreateRequestDto dto, Guid actorId);

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
        Task<List<AdminDto>> GetAllAsync();
        Task<IEnumerable<string>> GetPermissionsAsync(Guid adminId);
    }
}