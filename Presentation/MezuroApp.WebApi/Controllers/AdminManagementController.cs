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
        public async Task<IActionResult> CreateAdmin([FromBody] AdminCreateRequestDto dto,bool superAdmin)
        {
            if (Invalid(out var bad)) return bad;

            return await HandleAsync(async () =>
            {
                var actorId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var data = await _service.CreateAsync(dto, actorId,superAdmin);

                return CreatedResponse<object>(null, data, "ADMIN_CREATED");
            });
        }

        
        [HttpPut("admins/update")]
        public async Task<IActionResult> UpdateAdmin([FromBody] AdminUpdateRequestDto dto)
        {
            try
            {
                var actorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(actorIdStr, out var actorId))
                    return BadRequestResponse("INVALID_ACTOR");

                var result = await _service.UpdateAdminAsync(dto, actorId);
                return OkResponse(result, "ADMIN_UPDATED");
            }
            catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
            catch { return ServerErrorResponse(); }
        }
        // ============================
        [HttpGet("admins")]
        public async Task<IActionResult> GetAllAdminsPaged(
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await HandleAsync(async () =>
            {
                var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var data = await _service.GetAllAdminsAsync(id,isActive, page, pageSize);

                return OkResponse(data, "ADMINS_RETURNED");
            });
        }

// ============================
// SET ADMIN ACTIVE
// PATCH: api/admin/manage/admins/{id}/active?value=true
// ============================
        [HttpPatch("admins/{id}/active")]
        public async Task<IActionResult> SetAdminActive([FromRoute] string id, [FromQuery] bool value)
        {
            return await HandleAsync(async () =>
            {
                var actorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(actorIdStr, out var actorId))
                    return BadRequestResponse("INVALID_ACTOR");

                await _service.SetAdminActiveAsync(id, value, actorId);

                return OkResponse<object>(null, "ADMIN_ACTIVE_STATUS_UPDATED");
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
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetAdminByEmail(string email)
        {
            return await HandleAsync(async () =>
            {
                var data = await _service.GetByEmailAsync(email);
                return OkResponse(data, "ADMIN_RESPONSE");
            });
        }
        [HttpDelete("admins/{id}")]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            try
            {
                var actorIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(actorIdStr, out var actorId))
                    return BadRequestResponse("INVALID_ACTOR");

                await _service.DeleteOrRevokeAdminAsync(id, actorId);
                return OkResponse<object>(null, "ADMIN_DELETED_SUCCESSFULLY");
            }
            catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
            catch { return ServerErrorResponse(); }
        }

        
    }
}
