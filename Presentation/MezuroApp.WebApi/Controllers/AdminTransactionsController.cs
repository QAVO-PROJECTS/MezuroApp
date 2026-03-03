using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Transaction;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/transactions")]

public class AdminTransactionsController : BaseApiController
{
    private readonly IAdminTransactionService _service;

    public AdminTransactionsController(IAdminTransactionService service)
        => _service = service;

    // GET /api/admin/transactions?Search=&Status=&PaymentMethod=&From=&To=&MinAmount=&MaxAmount=&Page=&PageSize=
    [HttpGet]
    [Authorize(Permissions.Transactions.Read)]
    public async Task<IActionResult> Get([FromQuery] AdminTransactionListFilterDto f, CancellationToken ct)
    {
        try
        {
            var res = await _service.GetTransactionsAsync(f, ct);
            return OkResponse(res, "TRANSACTIONS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }

    }   

    [HttpGet("dashboard")]
    [Authorize(Permissions.Transactions.Read)]
    public async Task<IActionResult> Dashboard([FromQuery] AdminTransactionListFilterDto f, CancellationToken ct)
    {
        try
        {
            var res = await _service.GetDashboardAsync(f, ct);
            return OkResponse(res, "TRANSACTION_DASHBOARD_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // GET /api/admin/transactions/{id}
    [HttpGet("{id}")]
    [Authorize(Permissions.Transactions.Read)]
    public async Task<IActionResult> Detail([FromRoute] string id, CancellationToken ct)
    {
        try
        {
            var res = await _service.GetTransactionDetailAsync(id, ct);
            return OkResponse(res, "TRANSACTION_DETAIL_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // POST /api/admin/transactions/refund  (modal Create Refund)
    [HttpPost("refund")]
    [Authorize(Permissions.Transactions.Update)]
    public async Task<IActionResult> Refund([FromBody] AdminCreateRefundDto dto, CancellationToken ct)
    {
        try
        {
            var res = await _service.AdminReverseEpointAsync(dto, ct);
            return OkResponse(res, "REFUND_CREATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}