using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Application.GlobalException;
using System.Security.Claims;

namespace MezuroApp.WebApi.Controllers.Admin
{
    [Route("api/admin/manage")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminManagementController : BaseApiController
    {
        private readonly IAdminService _service;

        public AdminManagementController(IAdminService service)
        {
            _service = service;
        }

        private async Task<IActionResult> HandleAsync(Func<Task<IActionResult>> action)
        {
            try { return await action(); }
            catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
            catch { return ServerErrorResponse(); }
        }

        private bool Invalid(out IActionResult bad)
        {
            if (ModelState.IsValid) { bad = null!; return false; }
            bad = BadRequestResponse("INVALID_INPUT");
            return true;
        }

        // ============================
        // CREATE ADMIN
        // ============================
        [HttpPost("create")]
        public async Task<IActionResult> CreateAdmin([FromBody] AdminCreateRequestDto dto)
        {
            if (Invalid(out var bad)) return bad;

            return await HandleAsync(async () =>
            {
                var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var data = await _service.CreateAsync(dto, actorId);

                return CreatedResponse<object>(null, data, "ADMIN_CREATED");
            });
        }

        // ============================
        // SET ALL PERMISSIONS (replace)
        // ============================
        [HttpPost("permissions/set")]
        public async Task<IActionResult> SetPermissions(SetPermissionsDto dto)
        {
            return await HandleAsync(async () =>
            {
                var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _service.SetPermissionsAsync(dto, actorId);

                return OkResponse<object>(null, "ADMIN_PERMISSIONS_SET");
            });
        }

        // ============================
        // ADD / REMOVE PERMISSIONS
        // ============================
        [HttpPost("permissions/update")]
        public async Task<IActionResult> UpdatePermissions(AddRemovePermissionsDto dto)
        {
            return await HandleAsync(async () =>
            {
                var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _service.AddRemovePermissionsAsync( dto, actorId);

                return OkResponse<object>(null, "ADMIN_PERMISSIONS_UPDATED");
            });
        }

        // ============================
        // GET ADMIN INFO
        // ============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAdmin(string id)
        {
            return await HandleAsync(async () =>
            {
                var data = await _service.GetByIdAsync(Guid.Parse(id));
                return OkResponse(data, "ADMIN_RESPONSE");
            });
        }
        // ============================
// GET ALL ADMINS
// ============================
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAdmins()
        {
            return await HandleAsync(async () =>
            {
                var data = await _service.GetAllAsync();
                return OkResponse(data, "ADMINS_RETURNED");
            });
        }
        
    }
}
