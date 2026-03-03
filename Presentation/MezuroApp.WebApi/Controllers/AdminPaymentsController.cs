using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Payment;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "SuperAdmin,OWNER")]
public class AdminPaymentsController : BaseApiController
{
    private readonly IPaymentService _payment;

    public AdminPaymentsController(IPaymentService payment)
    {
        _payment = payment;
    }

    [Authorize(Permissions.Transactions.Update)]

    [HttpPost("epoint/reverse")]
    public async Task<IActionResult> Reverse([FromBody] AdminReverseEpointDto dto, CancellationToken ct)
    {
        try
        {
            await _payment.AdminReverseEpointAsync(dto, ct);
            return OkResponse(true, "ADMIN_PAYMENT_REVERSED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}
