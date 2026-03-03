using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.GlobalException;
using System.Security.Claims;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.AbandonedCart;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Controllers
{
    [Route("api/admin/abandoned-carts")]

    public class AbandonedCartsAdminController : BaseApiController
    {
        private readonly IAbandonedCartAdminService _service;

        public AbandonedCartsAdminController(IAbandonedCartAdminService service)
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
        // STATS (Total + Potential)
        // GET: /api/admin/abandoned-carts/stats?Search=...&Status=...&CreatedFrom=dd.MM.yyyy...
        // ============================
        [Authorize(Permissions.AbandonedCarts.Read)]
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] AbandonedCartAdminFilter filter)
        {
            return await HandleAsync(async () =>
            {
                var data = await _service.GetStatsAsync(filter);
                return OkResponse(data, "ABANDONED_CART_STATS_RETURNED");
            });
        }

        // ============================
        // LIST (Paged)
        // GET: /api/admin/abandoned-carts?page=1&pageSize=20&Search=...&Recoverable=true...
        // ============================
        [Authorize(Permissions.AbandonedCarts.Read)]
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] AbandonedCartAdminFilter filter,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            return await HandleAsync(async () =>
            {
                var data = await _service.GetPagedAsync(filter, page, pageSize);
                return OkResponse(data, "ABANDONED_CARTS_RETURNED");
            });
        }

        // ============================
        // DETAIL
        // GET: /api/admin/abandoned-carts/{id}
        // ============================
        [Authorize(Permissions.AbandonedCarts.Read)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(string id)
        {
            return await HandleAsync(async () =>
            {
                var data = await _service.GetDetailAsync(id);
                return OkResponse(data, "ABANDONED_CART_DETAIL_RETURNED");
            });
        }
    }
}