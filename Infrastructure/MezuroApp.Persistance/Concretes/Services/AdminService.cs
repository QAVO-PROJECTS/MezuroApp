using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using MezuroApp.Domain.HelperEntities;
using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MezuroApp.Persistance.Concretes.Services
{
    public class AdminService : IAdminService
    {
        private const string ADMIN_ROLE = "Admin";
        private const string SUPERADMIN_ROLE = "SuperAdmin";

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminService> _logger;
        private readonly IMailService _mailService;
        private readonly ITokenService _tokenService;
        private readonly IAuditLogService _auditLog;

        // IMPORTANT: sahələrə doğru təyin!
        public AdminService(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IMapper mapper,
            ILogger<AdminService> logger,
            IMailService mailService,
            ITokenService tokenService,
            IAuditLogService auditLogService)
        {
            _userManager  = userManager;
            _roleManager  = roleManager;
            _mapper       = mapper;
            _logger       = logger;
            _mailService  = mailService;
            _tokenService = tokenService;
            _auditLog     = auditLogService;
        }

        public async Task<AdminCreateResponseDto> CreateAsync(AdminCreateRequestDto dto, Guid actorId)
        {
            // yalnız SuperAdmin yarada bilər
            var actor = await _userManager.FindByIdAsync(actorId.ToString())
                         ?? throw new GlobalAppException("Actor tapılmadı");

            if (!await IsSuperAdminAsync(actor))
                throw new GlobalAppException("Yalnız SuperAdmin admin yarada bilər.");

            var exists = await _userManager.FindByEmailAsync(dto.Email);
            if (exists != null)
                throw new GlobalAppException("Bu e-poçt ilə artıq istifadəçi mövcuddur.");

            // Rollar mövcud deyilsə, yarad
            if (!await _roleManager.RoleExistsAsync(ADMIN_ROLE))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(ADMIN_ROLE));

            // Yeni Admin obyektini yarat
            var admin = _mapper.Map<Admin>(dto);
            admin.Id = Guid.NewGuid();
            admin.IsSuperAdmin = false;

            var createResult = await _userManager.CreateAsync(admin, dto.Password);
            if (!createResult.Succeeded)
                throw new GlobalAppException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(admin, ADMIN_ROLE);

            // İlkin permission-lar
            if (dto.InitialPermissions != null && dto.InitialPermissions.Any())
            {
                foreach (var p in dto.InitialPermissions.Distinct())
                    await _userManager.AddClaimAsync(admin, new Claim(Permissions.ClaimType, p));
            }

   

            var roles = await _userManager.GetRolesAsync(admin);
            var claims = await _userManager.GetClaimsAsync(admin);
            return new AdminCreateResponseDto
            {
                Id = admin.Id.ToString(),
                Email = admin.Email!,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                PhoneNumber =  admin.PhoneNumber,
                EmailConfirmed = admin.EmailConfirmed,
                Roles = roles,
                Permissions = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value)
            };
        }

        public async Task<AdminLoginResponseDto> LoginAsync(AdminLoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                throw new GlobalAppException("İstifadəçi və ya şifrə yanlışdır!");

            // Yalnız Admin və ya SuperAdmin portala daxil ola bilər
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("Bu istifadəçinin admin portala giriş icazəsi yoxdur.");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new GlobalAppException("İstifadəçi və ya şifrə yanlışdır!");

            if (!user.EmailConfirmed)
                throw new GlobalAppException("Zəhmət olmasa e-poçtunuzu təsdiq edin.");

            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            var claims = await _userManager.GetClaimsAsync(user);
            user.LastLoginAt = DateTime.UtcNow;

            return new AdminLoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,
                Admin = new AdminDto
                {
                    Id = user.Id.ToString(),
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsSuperAdmin = await IsSuperAdminAsync(user),
                    Roles = roles,
                    PhoneNumber = user.PhoneNumber,
                    Permissions = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value),
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        public async Task ChangePasswordAsync(Guid adminId, AdminChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new GlobalAppException("Köhnə və yeni şifrə tələb olunur!");

            var user = await _userManager.FindByIdAsync(adminId.ToString())
                       ?? throw new GlobalAppException("İstifadəçi tapılmadı!");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("Bu funksiya yalnız adminlər üçündür.");

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new GlobalAppException($"Şifrə dəyişdirilə bilmədi: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        public async Task SendResetPasswordLinkAsync(AdminResetPasswordRequestDto dto)
        {
            var email = dto.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                throw new GlobalAppException("E-poçt tələb olunur!");

            var user = await _userManager.FindByEmailAsync(email)
                       ?? throw new GlobalAppException("Daxil edilən e-poçt ilə istifadəçi tapılmadı.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("Bu istifadəçi admin deyil.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var encodedEmail = WebUtility.UrlEncode(email);

            var baseUrl = string.IsNullOrWhiteSpace(dto.ResetPageBaseUrl)
                ? "https://admin.mezuro.az/reset-password"
                : dto.ResetPageBaseUrl.TrimEnd('/');

            var resetLink = $"{baseUrl}?token={encodedToken}&email={encodedEmail}";

            var html = BuildResetPasswordEmailHtml(
                resetLink,
                logoUrl: "https://i.ibb.co/rK8QSYZh/logo.png",
                illustrationUrl: "https://i.ibb.co/GQtkwmYn/push.png"
            );

            await _mailService.SendEmailAsync(new MailRequest
            {
                ToEmail = email,
                Subject = "Admin şifrə sıfırlama",
                Body = html
            });

            _logger.LogInformation("Admin reset link generated for {Email}", email);
        }

        public async Task ResetPasswordAsync(AdminResetPasswordConfirmDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Token))
                throw new GlobalAppException("E-poçt və token tələb olunur!");

            var user = await _userManager.FindByEmailAsync(dto.Email)
                       ?? throw new GlobalAppException("İstifadəçi tapılmadı.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("Bu istifadəçi admin deyil.");

            var decodedToken = WebUtility.UrlDecode(dto.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);

            if (!result.Succeeded)
                throw new GlobalAppException($"Şifrə dəyişdirilə bilmədi: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        public async Task UpdateProfileAsync(Guid adminId, AdminUpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(adminId.ToString())
                       ?? throw new GlobalAppException("İstifadəçi tapılmadı.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("Bu funksiya yalnız adminlər üçündür.");

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;

            // IdentityUser artıq PhoneNumber sahəsinə malikdir; sənin User-də də var – ziddiyyət olmasın!
            user.PhoneNumber = dto.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new GlobalAppException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task SetPermissionsAsync(SetPermissionsDto dto, Guid actorId)
        {
            // yalnız SuperAdmin başqalarının permission-larını dəyişə bilər
            var actor = await _userManager.FindByIdAsync(actorId.ToString())
                         ?? throw new GlobalAppException("Actor tapılmadı");
            if (!await IsSuperAdminAsync(actor))
                throw new GlobalAppException("Yalnız SuperAdmin icazələri dəyişə bilər.");

            var target = await _userManager.FindByIdAsync(dto.Id)
                         ?? throw new GlobalAppException("Hədəf istifadəçi tapılmadı.");

            var targetRoles = await _userManager.GetRolesAsync(target);
            if (!targetRoles.Contains(ADMIN_ROLE) && !targetRoles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("Hədəf istifadəçi admin deyil.");

            var existingClaims = (await _userManager.GetClaimsAsync(target))
                .Where(c => c.Type == Permissions.ClaimType).ToList();

            // mövcudları sil
            foreach (var c in existingClaims)
                await _userManager.RemoveClaimAsync(target, c);

            // yenilərini əlavə et
            foreach (var p in (dto.Permissions ?? Array.Empty<string>()).Distinct())
                await _userManager.AddClaimAsync(target, new Claim(Permissions.ClaimType, p));


        }

        public async Task AddRemovePermissionsAsync(AddRemovePermissionsDto dto, Guid actorId)
        {
            var actor = await _userManager.FindByIdAsync(actorId.ToString())
                         ?? throw new GlobalAppException("Actor tapılmadı");
            if (!await IsSuperAdminAsync(actor))
                throw new GlobalAppException("Yalnız SuperAdmin icazələri dəyişə bilər.");

            var target = await _userManager.FindByIdAsync(dto.Id)
                         ?? throw new GlobalAppException("Hədəf istifadəçi tapılmadı.");

            var targetRoles = await _userManager.GetRolesAsync(target);
            if (!targetRoles.Contains(ADMIN_ROLE) && !targetRoles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("Hədəf istifadəçi admin deyil.");

            var existing = await _userManager.GetClaimsAsync(target);
            var existingPerms = existing.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToHashSet();

            // Add
            foreach (var p in (dto.Add ?? Array.Empty<string>()).Distinct())
            {
                if (!existingPerms.Contains(p))
                    await _userManager.AddClaimAsync(target, new Claim(Permissions.ClaimType, p));
            }

            // Remove
            foreach (var p in (dto.Remove ?? Array.Empty<string>()).Distinct())
            {
                var claim = existing.FirstOrDefault(c => c.Type == Permissions.ClaimType && c.Value == p);
                if (claim != null)
                    await _userManager.RemoveClaimAsync(target, claim);
            }


        }

        public async Task<AdminDto> GetByIdAsync(Guid adminId)
        {
            var user = await _userManager.FindByIdAsync(adminId.ToString())
                       ?? throw new GlobalAppException("İstifadəçi tapılmadı.");

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var dto = _mapper.Map<AdminDto>(user);
            dto.Roles = roles;
            dto.Permissions = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value);

            return dto;
        }

        public async Task<List<AdminDto>> GetAllAsync()
        {
            // 1) Users-u tam materializə et ki, sonrakı sorğular stream zamanı üst-üstə düşməsin
            var users = await _userManager.Users
                .AsNoTracking()
                .ToListAsync();

            var result = new List<AdminDto>();

            // 2) Qətiyyən Task.WhenAll İSTİFADƏ ETMƏ!
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Yalnız Admin və SuperAdmin
                if (!roles.Contains("Admin"))
                    continue;

                var claims = await _userManager.GetClaimsAsync(user);

                var dto = _mapper.Map<AdminDto>(user);
                dto.Roles = roles.ToList();
                dto.IsSuperAdmin = roles.Contains("SuperAdmin");
                dto.Permissions = claims
                    .Where(c => c.Type == Permissions.ClaimType)
                    .Select(c => c.Value)
                    .ToList();

                result.Add(dto);
            }

            return result;
        }

        public async Task<IEnumerable<string>> GetPermissionsAsync(Guid adminId)
        {
            var user = await _userManager.FindByIdAsync(adminId.ToString())
                       ?? throw new GlobalAppException("İstifadəçi tapılmadı.");

            var claims = await _userManager.GetClaimsAsync(user);
            return claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value);
        }

        private static string BuildResetPasswordEmailHtml(string resetLink, string logoUrl, string illustrationUrl)
        {
            // sənin mövcud HTML templaten (yuxarıdakı ilə eyni)
            var template = @"<!DOCTYPE html>..."; // buraya sənin eyni HTML-in gəlir
            return template
                .Replace("{{RESETLINK}}", resetLink)
                .Replace("{{LOGOURL}}", logoUrl)
                .Replace("{{ILLUSTRATIONURL}}", illustrationUrl);
        }

        private async Task<bool> IsSuperAdminAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(SUPERADMIN_ROLE))
                return true;

            // həmçinin Admin entity-də IsSuperAdmin true ola bilər
            if (user is Admin a && a.IsSuperAdmin)
                return true;

            // və ya xüsusi claim
            var claims = await _userManager.GetClaimsAsync(user);
            return claims.Any(c => c.Type == "role" && c.Value == SUPERADMIN_ROLE);
        }
    }
}