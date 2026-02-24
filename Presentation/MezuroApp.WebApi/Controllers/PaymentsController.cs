using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Payment;
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    // START PAYMENT (guest də ola bilər)
    [HttpPost("epoint/start")]
    [AllowAnonymous]
    public async Task<IActionResult> Start([FromBody] StartEpointPaymentDto dto, CancellationToken ct)
    {
        try
        {
            var userId =
                User?.Identity?.IsAuthenticated == true
                    ? (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"))
                    : null;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            var res = await _service.StartEpointAsync(userId, dto, ip, ua, ct);
            return OkResponse(res, "PAYMENT_STARTED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // CALLBACK (Epoint buranı vuracaq)
    [HttpPost("epoint/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromForm] EpointCallbackDto dto, CancellationToken ct)
    {
        try
        {
            await _service.HandleEpointCallbackAsync(dto, ct);
            return OkResponse(true, "PAYMENT_CALLBACK_OK");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // STATUS (login user üçün footprint lazım deyil, guest üçün mütləqdir)
    [HttpGet("status/{orderId}")]
    [AllowAnonymous]
    public async Task<IActionResult> Status([FromRoute] string orderId, [FromQuery] string? footprintId, CancellationToken ct)
    {
        try
        {
            var userId =
                User?.Identity?.IsAuthenticated == true
                    ? (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"))
                    : null;

            var res = await _service.GetPaymentStatusAsync(userId, orderId, footprintId, ct);
            return OkResponse(res, "PAYMENT_STATUS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}