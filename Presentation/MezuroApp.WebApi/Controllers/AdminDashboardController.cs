using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Dashboard;
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminDashboardController : BaseApiController
{
    private readonly IAdminDashboardService _service;

    public AdminDashboardController(IAdminDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AdminDashboardFilterDto filter, CancellationToken ct)
    {
        try
        {
            var data = await _service.GetDashboardAsync(filter, ct);
            return OkResponse(data, "DASHBOARD_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}