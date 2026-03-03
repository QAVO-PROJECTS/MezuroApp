using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Order.AdminOrder;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/admin/orders")]

public class AdminOrdersController : BaseApiController
{
    private readonly IAdminOrderService _service;

    public AdminOrdersController(IAdminOrderService service)
    {
        _service = service;
    }

    [Authorize(Permissions.Orders.Read)]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminOrdersFilterDto filter, CancellationToken ct)
    {
        try
        {
            var res = await _service.GetOrdersAsync(filter, ct);
            return OkResponse(res, "ADMIN_ORDERS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // GET /api/admin/orders/{id}
    [Authorize(Permissions.Orders.Read)]
    [HttpGet("{orderId}")]
    public async Task<IActionResult> Detail([FromRoute] string orderId, CancellationToken ct)
    {
        try
        {
            var res = await _service.GetOrderDetailAsync(orderId, ct);
            return OkResponse(res, "ADMIN_ORDER_DETAIL_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // POST /api/admin/orders/{id}/cancel
    [Authorize(Permissions.Orders.Update)]
    [HttpDelete("{orderId}/cancel")]
    public async Task<IActionResult> Cancel([FromRoute] string orderId, [FromBody] AdminCancelOrderDto dto, CancellationToken ct)
    {
        try
        {
            await _service.CancelOrderAsync(orderId, dto?.AdminNote, ct);
            return OkResponse(true, "ADMIN_ORDER_CANCELLED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // POST /api/admin/orders/{id}/resend-confirmation
    [Authorize(Permissions.Orders.Update)]
    [HttpPost("{orderId}/resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromRoute] string orderId, CancellationToken ct)
    {
        try
        {
            await _service.ResendConfirmationAsync(orderId, ct);
            return OkResponse(true, "ADMIN_ORDER_CONFIRMATION_SENT");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [Authorize(Permissions.Orders.Update)]
    // POST /api/admin/orders/{id}/status
    [HttpPost("{orderId}/status")]
    public async Task<IActionResult> ChangeStatus([FromRoute] string orderId, [FromBody] AdminChangeOrderStatusDto dto, CancellationToken ct)
    {
        try
        {
            await _service.SetOrderStatusAsync(orderId, dto.NewStatus, ct);
            return OkResponse(true, "ADMIN_ORDER_STATUS_UPDATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}

// DTO-lar (istəsən ayrı fayla çıxart)
public sealed record AdminCancelOrderDto(string? AdminNote);
public sealed record AdminChangeOrderStatusDto(string NewStatus);