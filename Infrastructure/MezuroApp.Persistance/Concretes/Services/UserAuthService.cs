using System.Globalization;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MezuroApp.Application.Dtos.Auth;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Domain.Entities;
using MezuroApp.Application.GlobalException;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Google.Apis.Auth;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Http;

namespace MezuroApp.Persistance.Concretes.Services
{
    public class UserAuthService : IUserAuthService
        
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IMapper _mapper;
        private readonly ILogger<UserAuthService> _logger;
        private readonly IMailService _mailService;
        private readonly ITokenService _tokenService;
        private readonly IFileService _fileService;
        private readonly INewsletterService _newsletterService;
        private readonly IAuditLogService _auditLogService;

        public UserAuthService
            (
            UserManager<User> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IMapper mapper,
            ILogger<UserAuthService> logger,
            IMailService mailService,
            ITokenService tokenService,
            IFileService fileService,
            INewsletterService newsletterService,
            IAuditLogService auditLogService
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _logger = logger;
            _mailService = mailService;
            _tokenService = tokenService;
            _fileService = fileService;
            _newsletterService = newsletterService;
            _auditLogService = auditLogService;
        }
    
        // private readonly ITokenService _tokenService;

    
        private const string DEFAULT_RESET_PAGE_URL = "https://globalservices.az/reset-password";
        private const string DEFAULT_LOGO_URL = "https://i.ibb.co/rK8QSYZh/logo.png";
        private const string DEFAULT_ILLUSTRATION_URL = "https://i.ibb.co/GQtkwmYn/push.png";
        public async Task<RegisterResponseDto> Register(RegisterRequestDto registerRequestDto)
        {
            try
            {
                // Yeni istifadəçi obyekti yaratmaq
                var user = _mapper.Map<User>(registerRequestDto);
                user.Username = registerRequestDto.Email;

                // E-poçtun mövcudluğunu yoxlamaq
                var existingUser = await _userManager.FindByEmailAsync(registerRequestDto.Email);
                if (existingUser != null)
                {
                    throw new GlobalAppException("Bu e-poçt ilə artıq bir istifadəçi mövcuddur!");
                }

                // Yeni istifadəçi yaratmaq
                var result = await _userManager.CreateAsync(user, registerRequestDto.Password);
                if (!result.Succeeded)
                {
                    throw new GlobalAppException(
                        $"İstifadəçi yaradılmadı: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                // "Customer" rolu varsa, yeni istifadəçiyə təyin edirik
                var roleExists = await _roleManager.RoleExistsAsync("Customer");
                if (!roleExists)
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>("Customer"));
                }
        

                await _userManager.AddToRoleAsync(user, "Customer");
                // DI: private readonly INewsletterService _newsletterService;

                await _newsletterService.EnsureForCurrentUserAsync(user.Id.ToString(), null);
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
     
                var confirmationLink = $"https://sshss.com/confirm-email?userId={user.Id}&token={token}";

                // Email göndərmək üçün mail servisini istifadə edirik
                var mailRequest = new MailRequest()
                {
                    ToEmail = user.Email,
                    Subject = "Email Təsdiqi",

                    Body =
                        $"Zəhmət olmasa, hesabınızı təsdiqləmək üçün aşağıdakı linkə klikləyin: <a href='{confirmationLink}'>Təsdiqlə</a>"
                };
       
                    


            await _mailService.SendEmailAsync(mailRequest);
                // Qeydiyyat uğurlu oldu
                return new RegisterResponseDto
                {
                    
                    
                        UserId = user.Id.ToString(),
                        Email = user.Email,
                        EmailVerificationRequired = true
                    
                };
                
            }
            
            catch (GlobalAppException ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstifadəçi qeydiyyatı zamanı səhv baş verdi.");
                throw new GlobalAppException("Qeydiyyat zamanı gözlənilməz bir səhv baş verdi.", ex);
            }
        }

        public async Task EditProfileImage(string userId,IFormFile file)
        {
           var image= await _fileService.UploadFile(file,"user/profile");
           var user = await _userManager.FindByIdAsync(userId);
           if (user == null)
           {
               throw new GlobalAppException("USER_ID_NOT_FOUND");
           }
           user.ProfileImage = image;
           await _userManager.UpdateAsync(user);
           
           
        }

        public async Task DeleteProfileImage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            user.ProfileImage = null;
            await _userManager.UpdateAsync(user);
        }

        public async Task EditProfile(string userId, UpdateProfileDto updateProfileDto)
        {
             var user = await _userManager.FindByIdAsync(userId);
             const string dateFormat = "dd.MM.yyyy";

             // VALID FROM
             if (!string.IsNullOrWhiteSpace(updateProfileDto.Birthday))
             {
                 if (!DateTime.TryParseExact(updateProfileDto.Birthday,
                         dateFormat,
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.None,
                         out var parsedFrom))
                     throw new GlobalAppException("INVALID_DATE_FORMAT");

                 // MUST – PostgreSQL timestamptz only accepts UTC
                 user.Birthday= DateTime.SpecifyKind(parsedFrom.AddHours(-4), DateTimeKind.Utc);
                 
             }

             if (!string.IsNullOrWhiteSpace(updateProfileDto.FirstName))
             {
                 user.FirstName=updateProfileDto.FirstName;
             }

             if (!string.IsNullOrWhiteSpace(updateProfileDto.LastName))
             {
                 user.LastName=updateProfileDto.LastName;
             }

             if (!string.IsNullOrWhiteSpace(updateProfileDto.Email))
             {
                 user.Email=updateProfileDto.Email;
             }

             if (!string.IsNullOrWhiteSpace(updateProfileDto.PhoneNumber))
             {
                 user.PhoneNumber=updateProfileDto.PhoneNumber;
             }
          
             await _userManager.UpdateAsync(user);
             
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto loginDto)
        {
            // 1. İstifadəçini e-poçt ilə tapırıq
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            // 2. İstifadəçi yoxdursa və ya şifrə yanlışsa səhv atırıq
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                throw new GlobalAppException("İstifadəçi və ya şifrə yanlışdır!");
            }

            if (user.EmailConfirmed == false)
            {
                throw new GlobalAppException("Zehmet olmazsa mailinizi tesdiq edin!");
            }

            await _newsletterService.EnsureForCurrentUserAsync(user.Id.ToString(), null);
            // 3. İstifadəçi tapılıb və şifrə düzgün daxil edilib
            // Access Token və Refresh Token yaradırıq
            user.LastLoginAt = DateTime.UtcNow;
           await _userManager.UpdateAsync(user);
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            // 4. İstifadəçinin rolu və digər məlumatlarını alırıq
            var roles = await _userManager.GetRolesAsync(user);



            // 5. LoginResponseDto yaradıb geri qaytarırıq
            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600,  // Access tokenin müddəti 1 saatdır
                User = new UserDto
                {
                    Id = user.Id.ToString(),  // 'Guid' tipini 'string' formatına çeviririk
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
             
                }
            };
        }
            public async Task<GoogleLoginResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto)
        {
            try
            {
                // Google ID tokeni doğrulamaq
                var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);

                // İstifadəçini tapmaq və ya yaratmaq
                var user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = new User
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        OAuthProvider = "Google",
                        OAuthProviderId = payload.Subject,
                        EmailConfirmed = true,
                        EmailConfirmationTokenExpires = DateTime.UtcNow
                    };

                    await _userManager.CreateAsync(user);
                    await _userManager.AddToRoleAsync(user, "Customer"); // Role əlavə edilir
                }

           
                // Tokenləri generasiya etmək
                var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            

                // Cavab DTO-nu qaytarmaq
                return new GoogleLoginResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600, // Tokenin müddəti
                    User = new GoogleUserDto
                    {
                        Id = user.Id.ToString(),
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        OAuthProvider = "Google",
                        Roles = await _userManager.GetRolesAsync(user)
                        
                    }
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Google login failed", ex);
            }
        }
        public async Task SendResetPasswordLinkAsync(string email, string? resetPageBaseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new GlobalAppException("E-poçt tələb olunur!");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new GlobalAppException("Daxil edilən e-poçt ilə istifadəçi tapılmadı.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Customer"))
                throw new GlobalAppException("Bu istifadəçi 'Customer' roluna sahib deyil.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var encodedEmail = WebUtility.UrlEncode(email);

            var baseUrl = string.IsNullOrWhiteSpace(resetPageBaseUrl)
                ? DEFAULT_RESET_PAGE_URL
                : resetPageBaseUrl.TrimEnd('/');

            var resetLink = $"{baseUrl}?token={encodedToken}&email={encodedEmail}";

            // HTML-i doldur
            var html = BuildResetPasswordEmailHtml(
                resetLink: resetLink,
                logoUrl: DEFAULT_LOGO_URL,
                illustrationUrl: DEFAULT_ILLUSTRATION_URL
            );

            var mailRequest = new MailRequest
            {
                ToEmail = email,
                Subject = "Şifrə sıfırlama bağlantınız",
                Body = html
            };

            await _mailService.SendEmailAsync(mailRequest);
            _logger.LogInformation("Password reset link generated for {Email}", email);
        }

        // =========================
        // ✅ ŞİFRƏNİ YENİLƏ
        // =========================
        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OldPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new GlobalAppException("Köhnə və yeni şifrə tələb olunur!");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new GlobalAppException("İstifadəçi tapılmadı!");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Customer"))
                throw new GlobalAppException("Bu istifadəçi 'Istifadeci' roluna sahib deyil!");

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);

            if (!result.Succeeded)
                throw new GlobalAppException($"Şifrə dəyişdirilə bilmədi: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        public async Task<ProfileDto> GetProfile(string userId)
        {
           var user= await _userManager.FindByIdAsync(userId);
           return _mapper.Map<ProfileDto>(user);
        }

        public async Task ResetPasswordAsync(string email, string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                throw new GlobalAppException("E-poçt və token tələb olunur!");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new GlobalAppException("İstifadəçi tapılmadı.");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Custoemr"))
                throw new GlobalAppException("Bu istifadəçi 'Istifadəçi' roluna sahib deyil.");

            var decodedToken = WebUtility.UrlDecode(token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            if (!result.Succeeded)
                throw new GlobalAppException($"Şifrə dəyişdirilə bilmədi: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

                private static string BuildResetPasswordEmailHtml(string resetLink, string logoUrl, string illustrationUrl)
        {
            var template = @"<!DOCTYPE html>
<html lang=""az"">
<head>
  <meta charset=""utf-8"">
  <meta name=""x-apple-disable-message-reformatting"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>Şifrə sıfırlama</title>
</head>
<body style=""margin:0; padding:0; background:#f5f7fb;"">
  <!-- Preheader -->
  <div style=""display:none; max-height:0; overflow:hidden; opacity:0;"">Şifrənizi yeniləmək üçün bağlantı.</div>

  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
    <tr>
      <td align=""center"" style=""padding:24px"">
        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""600"" style=""max-width:600px; width:100%; background:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e6e9f2;"">
          <!-- Header -->
          <tr>
            <td style=""padding:20px 24px;"">
              <img src=""{{LOGO_URL}}"" width=""125"" alt=""Global Procurement Services"" style=""display:block; border:0; outline:none;"">
            </td>
          </tr>

          <!-- Blue divider -->
          <tr><td style=""height:6px; background:#2b5aa7;""></td></tr>

          <!-- Illustration -->
          <tr>
            <td align=""center"" style=""padding:28px 24px 8px;"">
              <img src=""{{ILLUSTRATION_URL}}"" width=""200"" alt="""" style=""display:block; width:200px; max-width:80%; height:auto; border:0; outline:none;"">
            </td>
          </tr>

          <!-- Title -->
          <tr>
            <td style=""padding:8px 24px 0; font:500 20px/1.3 -apple-system,Segoe UI,Roboto,Arial,Helvetica,sans-serif; color:#000; padding-top:24px"">
              Şifrə sıfırlama bağlantınız
            </td>
          </tr>

          <!-- Body -->
          <tr>
            <td style=""padding:12px 24px 20px; font:400 14px/1.6 -apple-system,Segoe UI,Roboto,Arial,Helvetica,sans-serif; color:#636363;"">
              Yeni şifrə təyin etmək üçün aşağıdakı düyməyə klikləyin. Əgər bu tələbi siz göndərməmisinizsə, bu e-poçtu nəzərə almayın.
            </td>
          </tr>

          <!-- Button -->
          <tr>
            <td align=""left"" style=""padding:0 24px 24px;"">
              <a href=""{{RESET_LINK}}"" target=""_blank""
                 style=""display:inline-block; text-decoration:none; background:#2b5aa7; color:#ffffff; font:600 14px/1 -apple-system,Segoe UI,Roboto,Arial,Helvetica,sans-serif; padding:14px 22px; border-radius:8px;"">
                Şifrəni yenilə
              </a>
            </td>
          </tr>

          <!-- Fallback link -->
          <tr>
            <td style=""padding:0 24px 28px; font:400 12px/1.6 -apple-system,Segoe UI,Roboto,Arial,Helvetica,sans-serif; color:#6b7280; word-break:break-word;"">
              Düymə açılmırsa, bu linki brauzerdə açın:<br>
              <a href=""{{RESET_LINK}}"" target=""_blank"" style=""color:#2b5aa7; text-decoration:underline;"">{{RESET_LINK}}</a>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""padding:16px 24px 24px; font:400 12px/1.6 -apple-system,Segoe UI,Roboto,Arial,Helvetica,sans-serif; color:#9aa3af; border-top:1px solid #eef1f6;"">
              Bu bildiriş avtomatik yaradılıb. Suallar üçün: support@MezuroAppservices.com
            </td>
          </tr>
        </table>

        <div style=""font:400 11px/1.6 -apple-system,Segoe UI,Roboto,Arial,Helvetica,sans-serif; color:#9aa3af; padding:16px 8px;"">
          © 2025 Global Procurement Services. Bütün hüquqlar qorunur.
        </div>
      </td>
    </tr>
  </table>
</body>
</html>";

            return template
                .Replace("{{RESET_LINK}}", resetLink)
                .Replace("{{LOGO_URL}}", logoUrl)
                .Replace("{{ILLUSTRATION_URL}}", illustrationUrl);
        }

    }
}
