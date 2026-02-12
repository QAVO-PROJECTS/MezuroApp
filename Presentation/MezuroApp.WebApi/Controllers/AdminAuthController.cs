using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;

namespace MezuroApp.WebApi.Controllers
{
    [Route("api/admin/auth")]
    public class AdminAuthController : BaseApiController
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminAuthController> _logger;
        private readonly UserManager<User> _userManager;

        public AdminAuthController(
            IAdminService adminService,
            ILogger<AdminAuthController> logger,
            UserManager<User> userManager)
        {
            _adminService = adminService;
            _logger = logger;
            _userManager = userManager;
        }

        // Wrapper (sənin UserAuthController-dəki ilə eyni)
        private async Task<IActionResult> HandleAsync(Func<Task<IActionResult>> action)
        {
            try { return await action(); }
            catch (GlobalAppException ex)
            {
                _logger.LogWarning(ex, "Business error");
                return BadRequestResponse(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                return ServerErrorResponse();
            }
        }

        private bool IsInvalid(out IActionResult badRequest)
        {
            if (ModelState.IsValid)
            {
                badRequest = null!;
                return false;
            }
            badRequest = BadRequestResponse("INVALID_INPUT");
            return true;
        }

        // ================================
        // LOGIN
        // ================================
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequestDto dto)
        {
            if (IsInvalid(out var bad)) return bad;

            return await HandleAsync(async () =>
            {
                var data = await _adminService.LoginAsync(dto);
                return OkResponse(data, "ADMIN_LOGIN_SUCCESS");
            });
        }

        // ================================
        // Admin PASSWORD CHANGE (self)
        // ================================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] AdminChangePasswordDto dto)
        {
            if (IsInvalid(out var bad)) return bad;

            return await HandleAsync(async () =>
            {
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (adminId == null)
                    return BadRequestResponse("USER_ID_NOT_FOUND");

                await _adminService.ChangePasswordAsync(Guid.Parse(adminId), dto);

                return OkResponse<object>(null, "ADMIN_PASSWORD_CHANGE_SUCCESS");
            });
        }

        // ================================
        // RESET PASSWORD LINK
        // ================================
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] AdminResetPasswordRequestDto dto)
        {
            if (IsInvalid(out var bad)) return bad;

            return await HandleAsync(async () =>
            {
                await _adminService.SendResetPasswordLinkAsync(dto);
                return OkResponse<object>(null, "ADMIN_RESET_LINK_SENT");
            });
        }

        // ================================
        // RESET PASSWORD CONFIRMATION
        // ================================
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordConfirmDto dto)
        {
            if (IsInvalid(out var bad)) return bad;

            return await HandleAsync(async () =>
            {
                await _adminService.ResetPasswordAsync(dto);
                return OkResponse<object>(null, "ADMIN_PASSWORD_RESET_SUCCESS");
            });
        }

        // ================================
        // GET MY PROFILE
        // ================================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            return await HandleAsync(async () =>
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var data = await _adminService.GetByIdAsync(Guid.Parse(id));
                return OkResponse(data, "ADMIN_ME_SUCCESS");
            });
        }

        // ================================
        // UPDATE MY PROFILE
        // ================================
        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] AdminUpdateProfileDto dto)
        {
            if (IsInvalid(out var bad)) return bad;

            return await HandleAsync(async () =>
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _adminService.UpdateProfileAsync(Guid.Parse(id), dto);
                return OkResponse<object>(null, "PROFILE_UPDATE_SUCCESS");
            });
        }
    }
}