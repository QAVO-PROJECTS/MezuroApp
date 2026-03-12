using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Application.Dtos.Auth;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;

namespace MezuroApp.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class UserAuthController : BaseApiController
    {
        private readonly IUserAuthService _userAuthService;
        private readonly ILogger<UserAuthController> _logger;
        private readonly UserManager<User> _userManager;

        public UserAuthController(
            IUserAuthService userAuthService,
            ILogger<UserAuthController> logger,
            UserManager<User> userManager)
        {
            _userAuthService = userAuthService;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Helper: Təkrarsız try/catch üçün ümumi wrapper
        /// </summary>
        private async Task<IActionResult> HandleAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (GlobalAppException ex)
            {
                _logger.LogWarning(ex, "Biznes xətası.");
                // GlobalAppException mesajı dictionary açarı olmaya bilər, ona görə plain olaraq qaytarırıq
                return BadRequestResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gözlənilməz server xətası.");
                return ServerErrorResponse(); // "SERVER_ERROR" lokallaşdırılır
            }
        }

        private bool IsModelInvalid(out IActionResult badRequest)
        {
            if (ModelState.IsValid)
            {
                badRequest = null!;
                return false;
            }

            badRequest = BadRequestResponse("INVALID_INPUT");
            return true;
        }

        /// <summary>Yeni istifadəçi qeydiyyatı</summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;

            return await HandleAsync(async () =>
            {
                var result = await _userAuthService.Register(dto);
                return CreatedResponse<object>(null, result, "REGISTER_SUCCESS");
            });
        }

        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [HttpPatch("edit-profile-image")]
        public async Task<IActionResult> EditProfileImage([FromForm]  EditProfileImageRequest request)
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;
            return await HandleAsync(async () =>
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequestResponse("USER_ID_NOT_FOUND");
                await _userAuthService.EditProfileImage(userId, request.Image);
                return OkResponse("", "EDIT_PROFILE_SUCCESS");
            });

        }

        [Authorize(Roles = "Customer")]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;
            return await HandleAsync(async () =>
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequestResponse("USER_ID_NOT_FOUND");
                var user = await _userAuthService.GetProfile(userId);
                return OkResponse(user, "GET_PROFILE_SUCCESS");
            });
        }
        [Authorize(Roles = "Customer")]
        [HttpPut("edit-profile")]
        public async Task<IActionResult> EditUserProfile(UpdateProfileDto dto)
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;
            return await HandleAsync(async () =>
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequestResponse("USER_ID_NOT_FOUND");
                await _userAuthService.EditProfile(userId,dto);
                return OkResponse("", "EDIT_PROFILE_SUCCESS");
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpDelete("delete-profile-image")]
        public async Task<IActionResult> DeeleteProfileImage()
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;
            return await HandleAsync(async () =>
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequestResponse("USER_ID_NOT_FOUND");
                await _userAuthService.DeleteProfileImage(userId);
                return OkResponse("", "EDIT_PROFILE_SUCCESS");
            });

        }
        /// <summary>İstifadəçi login</summary>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;

            return await HandleAsync(async () =>
            {
                var data = await _userAuthService.Login(dto, GetIpAddress());
                return OkResponse(data, "LOGIN_SUCCESS");
            });
        }
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequestResponse("REFRESH_TOKEN_REQUIRED");

            return await HandleAsync(async () =>
            {
                var data = await _userAuthService.RefreshTokenAsync(dto.RefreshToken, GetIpAddress());
                return OkResponse(data, "TOKEN_REFRESH_SUCCESS");
            });
        }
        [Authorize(Roles = "Customer")]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequestResponse("REFRESH_TOKEN_REQUIRED");

            return await HandleAsync(async () =>
            {
                await _userAuthService.RevokeRefreshTokenAsync(dto.RefreshToken, GetIpAddress());
                return OkResponse<object>(null, "TOKEN_REVOKED_SUCCESS");
            });
        }

        /// <summary>Google login</summary>
        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.IdToken))
                return BadRequestResponse("GOOGLE_ID_TOKEN_REQUIRED");

            return await HandleAsync(async () =>
            {
                var data = await _userAuthService.GoogleLoginAsync(dto);
                return OkResponse(data, "GOOGLE_LOGIN_SUCCESS");
            });
        }

        /// <summary>E-poçtu təsdiqləyir</summary>
        [AllowAnonymous]
        [HttpGet("confirm/email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token, [FromQuery] string userId)
        {
            _logger.LogInformation("ConfirmEmail called with token: {Token}, userId: {UserId}", token, userId);

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("INVALID_TOKEN_OR_USERID");

            return await HandleAsync(async () =>
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return BadRequestResponse("INVALID_TOKEN_OR_USERID");

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Email confirmed successfully for userId: {UserId}", userId);
                    return OkResponse<object>(null, "EMAIL_CONFIRMED");
                }

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Email confirmation failed for userId: {UserId}, errors: {Errors}", userId, errors);

                // Dinamik mesaj: Localize + detallar
                var msg = $"{LocalizeAll("EMAIL_CONFIRM_FAILED")}: {errors}";
                return BadRequestResponse(msg);
            });
        }

        /// <summary>Şifrəni unutdum (reset linki göndər)</summary>
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;

            return await HandleAsync(async () =>
            {
                await _userAuthService.SendResetPasswordLinkAsync(dto.Email);
                _logger.LogInformation("Password reset link requested for {Email}", dto.Email);
                return OkResponse<object>(null, "RESET_LINK_SENT");
            });
        }

        /// <summary>Maildən gələn linklə şifrəni yenilə</summary>
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;

            return await HandleAsync(async () =>
            {
                await _userAuthService.ResetPasswordAsync(dto.Email, dto.Token, dto.NewPassword);
                return OkResponse<object>(null, "PASSWORD_RESET_SUCCESS");
            });
        }

        /// <summary>İstifadəçi parolunu dəyişir</summary>
        [Authorize(Roles = "Customer")]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (IsModelInvalid(out var badRequest)) return badRequest;

            return await HandleAsync(async () =>
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequestResponse("USER_ID_NOT_FOUND");

                await _userAuthService.ChangePasswordAsync(userId, dto);
                return OkResponse<object>(null, "PASSWORD_CHANGE_SUCCESS");
            });
        }
        private string? GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
