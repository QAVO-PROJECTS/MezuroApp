using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Dtos.Audit;
using MezuroApp.Application.GlobalException;
using MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize] // səndə permission policy varsa onu da əlavə et
public class AdminAuditLogsController : BaseApiController
{
    private readonly AdminAuditLogService _svc;

    public AdminAuditLogsController(AdminAuditLogService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AdminAuditLogFilterDto f, CancellationToken ct)
    {
        try
        {
            var data = await _svc.GetAsync(f, ct);
            return OkResponse(data, "ADMIN_ACTIVITY_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        
    }
}