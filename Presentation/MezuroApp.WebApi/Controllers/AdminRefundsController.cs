using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;

using MezuroApp.Application.Dtos.Order.AdminOrder; // öz namespace-ni qoy
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/admin/refunds")]
[Authorize(Roles = "SuperAdmin,OWNER")]
public class AdminRefundsController : BaseApiController
{
    private readonly IAdminRefundService _service;

    public AdminRefundsController(IAdminRefundService service)
    {
        _service = service;
    }

    // GET /api/admin/refunds?Search=...&Status=...&Page=1&PageSize=20
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminRefundListFilterDto filter, CancellationToken ct)
    {
        try
        {
            var res = await _service.GetRefundsAsync(filter, ct);
            return OkResponse(res, "ADMIN_REFUNDS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // GET /api/admin/refunds/{paymentTransactionId}
    [HttpGet("{paymentTransactionId}")]
    public async Task<IActionResult> Detail([FromRoute] string paymentTransactionId, CancellationToken ct)
    {
        try
        {
            var res = await _service.GetRefundDetailAsync(paymentTransactionId, ct);
            return OkResponse(res, "ADMIN_REFUND_DETAIL_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}