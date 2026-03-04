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
        private readonly IAuditHelper _audit;

        // IMPORTANT: sahələrə doğru təyin!
        public AdminService(
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IMapper mapper,
            ILogger<AdminService> logger,
            IMailService mailService,
            ITokenService tokenService,
            IAuditHelper audit)
        {
            _userManager  = userManager;
            _roleManager  = roleManager;
            _mapper       = mapper;
            _logger       = logger;
            _mailService  = mailService;
            _tokenService = tokenService;
            _audit = audit;
        }
        public async Task<PagedResult<AdminDto>> GetAllAdminsAsync(string id, bool ? isActive, int page = 1, int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            // yalnız Admin və SuperAdmin (DB-də role join etmədən, sadə/yüngül yol)
            // əvvəlcə bütün user-ları çəkib sonra role yoxlama edirik (Identity join-ları qarışdırmırıq)
            var users = await _userManager.Users
                .AsNoTracking().Where(x=>x.Id.ToString()!=id && !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var list = new List<AdminDto>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
                    continue;

                if (isActive.HasValue && u.IsActive != isActive.Value)
                    continue;

                var claims = await _userManager.GetClaimsAsync(u);

                list.Add(new AdminDto
                {
                    Id = u.Id.ToString(),
                    Email = u.Email!,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber,
                    Roles = roles,
                    IsActive = u.IsActive,
                    IsSuperAdmin = roles.Contains(SUPERADMIN_ROLE) || (u is Admin aa && aa.IsSuperAdmin),
                    Permissions = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToList(),
                    LastLoginAt = u.LastLoginAt?.ToString("dd.MM.yyyy HH:mm"),
                    CreatedAt = u.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                });
            }

            var total = list.Count;

            var items = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<AdminDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task SetAdminActiveAsync(string adminId, bool value, Guid actorId)
        {
            var actor = await _userManager.FindByIdAsync(actorId.ToString())
                        ?? throw new GlobalAppException("ACTOR_NOT_FOUND");

            if (!await IsSuperAdminAsync(actor))
                throw new GlobalAppException("ONLY_SUPERADMIN_CAN_UPDATE_ADMIN");

            if (!Guid.TryParse(adminId, out var gid))
                throw new GlobalAppException("INVALID_ADMIN_ID");

            var target = await _userManager.FindByIdAsync(gid.ToString())
                         ?? throw new GlobalAppException("TARGET_ADMIN_NOT_FOUND");
         

            var roles = await _userManager.GetRolesAsync(target);
            if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
                throw new GlobalAppException("TARGET_USER_IS_NOT_ADMIN");

            // istəsən: superadmin-i deactivate etməyi qadağan et
            // if (roles.Contains(SUPERADMIN_ROLE) && value == false)
            //     throw new GlobalAppException("CANNOT_DEACTIVATE_SUPERADMIN");

            var oldValues = new Dictionary<string, object>
            {
                ["IsActive"] = target.IsActive
            };
            target.IsActive = value;
            target.UpdatedAt = DateTime.UtcNow;

            var res = await _userManager.UpdateAsync(target);
            if (!res.Succeeded)
                throw new GlobalAppException("ADMIN_UPDATE_FAILED");
            await _audit.LogAsync(
                "Admins",
                "UPDATE",
                value ? "ADMIN_ACTIVATED" : "ADMIN_DEACTIVATED",
                target.Id,
                oldValues,
                new Dictionary<string, object>
                {
                    ["IsActive"] = target.IsActive,
                    ["TargetEmail"] = target.Email ?? ""
                }
            );
        }
        public async Task<AdminCreateResponseDto> CreateAsync(AdminCreateRequestDto dto, Guid actorId , bool superAdmin)
        {
            // yalnız SuperAdmin yarada bilər
            var actor = await _userManager.FindByIdAsync(actorId.ToString())
                         ?? throw new GlobalAppException("Actor tapılmadı");

            if (!await IsSuperAdminAsync(actor))
                throw new GlobalAppException("Yalnız SuperAdmin admin yarada bilər.");

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                // artıq admin rolundadırsa xəta at
                var role = await _userManager.GetRolesAsync(existingUser);
                if (role.Contains(ADMIN_ROLE) || role.Contains(SUPERADMIN_ROLE))
                    throw new GlobalAppException("EMAIL_ALREADY_ADMIN");

                // mövcud user-i admin et
                if (superAdmin)
                {
                    await _userManager.AddToRoleAsync(existingUser, SUPERADMIN_ROLE);
                }
                else
                {
                    await _userManager.AddToRoleAsync(existingUser, ADMIN_ROLE);
                }
                if (superAdmin)
                {
                    // ✅ superadmin yaradılırsa: hamı permissions
                    foreach (var p in GetAllPermissions())
                        await _userManager.AddClaimAsync(existingUser, new Claim(Permissions.ClaimType, p));
                }
                else if (dto.InitialPermissions != null && dto.InitialPermissions.Any())
                {
                    foreach (var p in dto.InitialPermissions.Distinct())
                        await _userManager.AddClaimAsync(existingUser, new Claim(Permissions.ClaimType, p));
                }
                return new AdminCreateResponseDto
                {
                    Id = existingUser.Id.ToString(),
                    Email = existingUser.Email!,
                    FirstName = existingUser.FirstName,
                    LastName = existingUser.LastName,
                    PhoneNumber = existingUser.PhoneNumber,
                    EmailConfirmed = existingUser.EmailConfirmed,
                    Roles = await _userManager.GetRolesAsync(existingUser),
                    Permissions = new List<string>()
                };
            }

            // Rollar mövcud deyilsə, yarad
            if (!await _roleManager.RoleExistsAsync(ADMIN_ROLE))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(ADMIN_ROLE));
            if (!await _roleManager.RoleExistsAsync(SUPERADMIN_ROLE))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(SUPERADMIN_ROLE));
            

            // Yeni Admin obyektini yarat
            var admin = _mapper.Map<Admin>(dto);
            admin.Id = Guid.NewGuid();
            admin.IsSuperAdmin = false;

            var createResult = await _userManager.CreateAsync(admin, dto.Password);
            if (!createResult.Succeeded)
                throw new GlobalAppException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            if (superAdmin)
            {
                await _userManager.AddToRoleAsync(admin, SUPERADMIN_ROLE);
            }
            else
            {
                await _userManager.AddToRoleAsync(admin, ADMIN_ROLE);
            }

            // İlkin permission-lar
            if (superAdmin)
            {
                // ✅ superadmin yaradılırsa: hamı permissions
                foreach (var p in GetAllPermissions())
                    await _userManager.AddClaimAsync(admin, new Claim(Permissions.ClaimType, p));
            }
            else if (dto.InitialPermissions != null && dto.InitialPermissions.Any())
            {
                foreach (var p in dto.InitialPermissions.Distinct())
                    await _userManager.AddClaimAsync(admin, new Claim(Permissions.ClaimType, p));
            }

   

            var roles = await _userManager.GetRolesAsync(admin);
            var claims = await _userManager.GetClaimsAsync(admin);
            await _audit.LogAsync(
                "Admins",
                "CREATE",
                superAdmin ? "SUPERADMIN_CREATED" : "ADMIN_CREATED",
                admin.Id,
                null,
                new Dictionary<string, object>
                {
                    ["Email"] = admin.Email ?? "",
                    ["FirstName"] = admin.FirstName ?? "",
                    ["LastName"] = admin.LastName ?? "",
                    ["PhoneNumber"] = admin.PhoneNumber ?? "",
                    ["Role"] = superAdmin ? SUPERADMIN_ROLE : ADMIN_ROLE,
                    ["InitialPermissionsCount"] = superAdmin ? GetAllPermissions().Count() : (dto.InitialPermissions?.Distinct().Count() ?? 0)
                }
            );
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

        public async Task<AdminUpdateResponseDto> UpdateAdminAsync(AdminUpdateRequestDto dto, Guid actorId)
{
    // actor superadmin olmalıdır
    var actor = await _userManager.FindByIdAsync(actorId.ToString())
                ?? throw new GlobalAppException("Actor tapılmadı");

    if (!await IsSuperAdminAsync(actor))
        throw new GlobalAppException("Yalnız SuperAdmin admin update edə bilər.");

    if (!Guid.TryParse(dto.Id, out var targetId))
        throw new GlobalAppException("INVALID_ADMIN_ID");

    var target = await _userManager.FindByIdAsync(targetId.ToString())
                 ?? throw new GlobalAppException("Hədəf admin tapılmadı.");
    

    // target admin/superadmin olmalıdır
    var targetRoles = await _userManager.GetRolesAsync(target);
    if (!targetRoles.Contains(ADMIN_ROLE) && !targetRoles.Contains(SUPERADMIN_ROLE))
        throw new GlobalAppException("Hədəf istifadəçi admin deyil.");
    var oldValues = new Dictionary<string, object>
    {
        ["Email"] = target.Email ?? "",
        ["FirstName"] = target.FirstName ?? "",
        ["LastName"] = target.LastName ?? "",
        ["PhoneNumber"] = target.PhoneNumber ?? "",
        ["IsSuperAdmin"] = targetRoles.Contains(SUPERADMIN_ROLE) || (target is Admin b && b.IsSuperAdmin),
        ["Roles"] = string.Join(",", targetRoles),
    };
    
    

    // ===== basic fields (partial) =====
    if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.Trim() != target.Email)
    {
        // email unique
        var exists = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (exists != null && exists.Id != target.Id)
            throw new GlobalAppException("Bu e-poçt ilə artıq istifadəçi mövcuddur.");

        // Identity üçün düzgün set
        var setEmailRes = await _userManager.SetEmailAsync(target, dto.Email.Trim());
        if (!setEmailRes.Succeeded)
            throw new GlobalAppException(string.Join(", ", setEmailRes.Errors.Select(e => e.Description)));

        target.UserName = dto.Email.Trim(); // əgər username email-dirsə
        target.NormalizedUserName = dto.Email.Trim().ToUpperInvariant();
    }

    if (dto.FirstName != null) target.FirstName = dto.FirstName;
    if (dto.LastName != null) target.LastName = dto.LastName;
    if (dto.PhoneNumber != null) target.PhoneNumber = dto.PhoneNumber;

    // ===== role switch (Admin <-> SuperAdmin) =====
    if (dto.MakeSuperAdmin.HasValue)
    {
        var make = dto.MakeSuperAdmin.Value;

        // self-demotion qadağası (istəsən saxla)
        if (target.Id == actor.Id && make == false)
            throw new GlobalAppException("Öz SuperAdmin rolunu silə bilməzsən.");

        if (make)
        {
            if (!targetRoles.Contains(SUPERADMIN_ROLE))
            {
                await _userManager.RemoveFromRoleAsync(target, ADMIN_ROLE);
                await _userManager.AddToRoleAsync(target, SUPERADMIN_ROLE);
            }

            // entity-də flag varsa
            if (target is Admin a) a.IsSuperAdmin = true;

            // superadmin oldu → bütün icazələr ver
            var existingClaims = await _userManager.GetClaimsAsync(target);
            var existingPerms = existingClaims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var p in GetAllPermissions())
                if (!existingPerms.Contains(p))
                    await _userManager.AddClaimAsync(target, new Claim(Permissions.ClaimType, p));
        }
        
        else
        {
            if (!targetRoles.Contains(ADMIN_ROLE))
            {
                await _userManager.RemoveFromRoleAsync(target, SUPERADMIN_ROLE);
                await _userManager.AddToRoleAsync(target, ADMIN_ROLE);
            }

            if (target is Admin a) a.IsSuperAdmin = false;
        }
        
    }
    

    // ===== permissions update =====
    // ReplaceAllPermissions = true → hamısını sil, yenisini yaz
    if (dto.ReplaceAllPermissions == true)
    {
        var existingClaims = (await _userManager.GetClaimsAsync(target))
            .Where(c => c.Type == Permissions.ClaimType)
            .ToList();

        foreach (var c in existingClaims)
            await _userManager.RemoveClaimAsync(target, c);

        foreach (var p in (dto.Permissions ?? new List<string>()).Distinct())
            await _userManager.AddClaimAsync(target, new Claim(Permissions.ClaimType, p));
    }
    else
    {
        // Add / Remove (opsional)
        var existing = await _userManager.GetClaimsAsync(target);
        var existingPerms = existing.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToHashSet();

        if (dto.AddPermissions != null)
        {
            foreach (var p in dto.AddPermissions.Distinct())
                if (!existingPerms.Contains(p))
                    await _userManager.AddClaimAsync(target, new Claim(Permissions.ClaimType, p));
        }

        if (dto.RemovePermissions != null)
        {
            foreach (var p in dto.RemovePermissions.Distinct())
            {
                var claim = existing.FirstOrDefault(c => c.Type == Permissions.ClaimType && c.Value == p);
                if (claim != null)
                    await _userManager.RemoveClaimAsync(target, claim);
            }
        }
    }

    target.UpdatedAt = DateTime.UtcNow;

    // ✅ ən vacib hissə: DB-yə yaz
    var updateRes = await _userManager.UpdateAsync(target);
    if (!updateRes.Succeeded)
        throw new GlobalAppException(string.Join(", ", updateRes.Errors.Select(e => e.Description)));
    

    // response
    var rolesAfter = await _userManager.GetRolesAsync(target);
    var claimsAfter = await _userManager.GetClaimsAsync(target);
    var newValues = new Dictionary<string, object>
    {
        ["Email"] = target.Email ?? "",
        ["FirstName"] = target.FirstName ?? "",
        ["LastName"] = target.LastName ?? "",
        ["PhoneNumber"] = target.PhoneNumber ?? "",
        ["IsSuperAdmin"] = rolesAfter.Contains(SUPERADMIN_ROLE) || (target is Admin bb && bb.IsSuperAdmin),
        ["Roles"] = string.Join(",", rolesAfter),
        ["PermissionsCount"] = claimsAfter.Count(c => c.Type == Permissions.ClaimType)
    };
    await _audit.LogAsync(
        "Admins",
        "UPDATE",
        "ADMIN_UPDATED",
        target.Id,
        oldValues,
        newValues
    );

    return new AdminUpdateResponseDto
    {
        Id = target.Id.ToString(),
        Email = target.Email!,
        FirstName = target.FirstName,
        LastName = target.LastName,
        PhoneNumber = target.PhoneNumber,
        EmailConfirmed = target.EmailConfirmed,
        IsSuperAdmin = rolesAfter.Contains(SUPERADMIN_ROLE) || (target is Admin aa && aa.IsSuperAdmin),
        Roles = rolesAfter,
        Permissions = claimsAfter.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).Distinct()
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
            if (user.IsActive!=true)
            {
                throw new GlobalAppException("Yalniz aktiv adminler sisteme daxil ola biler!");
            }

            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);
            

            var claims = await _userManager.GetClaimsAsync(user);
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

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
                    LastLoginAt = user.LastLoginAt?.ToString("dd-MMM-yyyy HH:mm"),
                    CreatedAt = user.CreatedAt.ToString("dd-MMM-yyyy HH:mm"),
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
            var before = await _userManager.GetClaimsAsync(target);
            var beforePerms = before.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToList();

            // mövcudları sil
            foreach (var c in existingClaims)
                await _userManager.RemoveClaimAsync(target, c);

            // yenilərini əlavə et
            foreach (var p in (dto.Permissions ?? Array.Empty<string>()).Distinct())
                await _userManager.AddClaimAsync(target, new Claim(Permissions.ClaimType, p));
            await _audit.LogAsync(
                "Admins",
                "UPDATE",
                "ADMIN_PERMISSIONS_REPLACED",
                target.Id,
                new Dictionary<string, object>
                {
                    ["BeforePermissions"] = beforePerms,
                    ["BeforeCount"] = beforePerms.Count
                },
                new Dictionary<string, object>
                {
                    ["AfterPermissions"] = (dto.Permissions ?? Array.Empty<string>()).Distinct().ToList(),
                    ["AfterCount"] = (dto.Permissions ?? Array.Empty<string>()).Distinct().Count()
                }
            );


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
            var before = await _userManager.GetClaimsAsync(target);
            var beforePerms = before.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value).ToHashSet();

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
            await _audit.LogAsync(
                "Admins",
                "UPDATE",
                "ADMIN_PERMISSIONS_PATCHED",
                target.Id,
                new Dictionary<string, object>
                {
                    ["BeforeCount"] = beforePerms.Count
                },
                new Dictionary<string, object>
                {
                    ["Add"] = (dto.Add ?? Array.Empty<string>()).Distinct().ToList(),
                    ["Remove"] = (dto.Remove ?? Array.Empty<string>()).Distinct().ToList(),
                    ["AfterCount"] = (await _userManager.GetClaimsAsync(target))
                        .Count(c => c.Type == Permissions.ClaimType)
                }
            );


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
        public async Task<AdminDto> GetByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email)
                       ?? throw new GlobalAppException("İstifadəçi tapılmadı.");

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var dto = _mapper.Map<AdminDto>(user);
            dto.Roles = roles;
            dto.Permissions = claims.Where(c => c.Type == Permissions.ClaimType).Select(c => c.Value);

            return dto;
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
        
   public async Task DeleteOrRevokeAdminAsync(string adminId, Guid actorId)
{
    var actor = await _userManager.FindByIdAsync(actorId.ToString())
                ?? throw new GlobalAppException("ACTOR_NOT_FOUND");

    if (!await IsSuperAdminAsync(actor))
        throw new GlobalAppException("ONLY_SUPERADMIN_CAN_DELETE_ADMIN");

    if (!Guid.TryParse(adminId, out var gid))
        throw new GlobalAppException("INVALID_ADMIN_ID");

    var target = await _userManager.FindByIdAsync(gid.ToString())
                 ?? throw new GlobalAppException("TARGET_ADMIN_NOT_FOUND");

    var roles = await _userManager.GetRolesAsync(target);

    if (!roles.Contains(ADMIN_ROLE) && !roles.Contains(SUPERADMIN_ROLE))
        throw new GlobalAppException("TARGET_USER_IS_NOT_ADMIN");
    var oldValues = new Dictionary<string, object>
    {
        ["Email"] = target.Email ?? "",
        ["IsActive"] = target.IsActive,
        ["IsDeleted"] = target.IsDeleted,
        ["Roles"] = string.Join(",", roles)
    };

    // özünü silməsin
    if (target.Id == actor.Id)
        throw new GlobalAppException("CANNOT_DELETE_YOURSELF");

    // ===== USER-DIRMI? (admin rolundan başqa role var?) =====
    var hasOtherRoles = roles.Any(r => r != ADMIN_ROLE && r != SUPERADMIN_ROLE);

    if (hasOtherRoles)
    {
        // 🔹 Sadəcə admin rolu və permission-lar silinir

        if (roles.Contains(ADMIN_ROLE))
            await _userManager.RemoveFromRoleAsync(target, ADMIN_ROLE);

        if (roles.Contains(SUPERADMIN_ROLE))
            await _userManager.RemoveFromRoleAsync(target, SUPERADMIN_ROLE);

        var claims = await _userManager.GetClaimsAsync(target);
        var adminClaims = claims
            .Where(c => c.Type == Permissions.ClaimType)
            .ToList();

        foreach (var c in adminClaims)
            await _userManager.RemoveClaimAsync(target, c);

        target.IsActive = false;
        target.UpdatedAt = DateTime.UtcNow;

        var update = await _userManager.UpdateAsync(target);
        if (!update.Succeeded)
            throw new GlobalAppException("ADMIN_UPDATE_FAILED");
        await _audit.LogAsync(
            "Admins",
            "DELETE",
            "ADMIN_REVOKED",
            target.Id,
            oldValues,
            new Dictionary<string, object>
            {
                ["IsActive"] = target.IsActive,
                ["IsDeleted"] = target.IsDeleted,
                ["RemovedRoles"] = new [] { ADMIN_ROLE, SUPERADMIN_ROLE },
                ["RemovedPermissions"] = true
            }
        );
    }
    else
    {
        // 🔴 Yalnız Admin-dirsə → Soft Delete

        target.IsDeleted = true;
        target.IsActive = false;
        target.DeletedDate = DateTime.UtcNow;
        target.UpdatedAt = DateTime.UtcNow;

        // E-mail və username dəyişdir ki conflict olmasın
        var uniqueSuffix = $"_deleted_{Guid.NewGuid().ToString("N")[..6]}";

        target.Email = target.Email + uniqueSuffix;
        target.UserName = target.UserName + uniqueSuffix;
        target.NormalizedEmail = target.Email.ToUpperInvariant();
        target.NormalizedUserName = target.UserName.ToUpperInvariant();

        var update = await _userManager.UpdateAsync(target);
        if (!update.Succeeded)
            throw new GlobalAppException("ADMIN_DELETE_FAILED");
        await _audit.LogAsync(
            "Admins",
            "DELETE",
            "ADMIN_SOFT_DELETED",
            target.Id,
            oldValues,
            new Dictionary<string, object>
            {
                ["IsActive"] = target.IsActive,
                ["IsDeleted"] = target.IsDeleted,
                ["DeletedDate"] = target.DeletedDate?.ToString("O") ?? "",
                ["EmailAfter"] = target.Email ?? "",
                ["UserNameAfter"] = target.UserName ?? ""
            }
        );
    }
}
        private static IEnumerable<string> GetAllPermissions()
        {
            // Permissions.Products.Read kimi nested class-lar varsa, hamısını yığır
            var type = typeof(Permissions);

            static IEnumerable<string> Collect(Type t)
            {
                var fields = t.GetFields(System.Reflection.BindingFlags.Public |
                                         System.Reflection.BindingFlags.Static |
                                         System.Reflection.BindingFlags.FlattenHierarchy)
                    .Where(f => f.FieldType == typeof(string))
                    .Select(f => (string)f.GetValue(null)!);

                var nested = t.GetNestedTypes(System.Reflection.BindingFlags.Public)
                    .SelectMany(Collect);

                return fields.Concat(nested);
            }

            return Collect(type).Distinct();
        }
    }
}