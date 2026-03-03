using MezuroApp.Application.Abstracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Dtos.AdminUsers;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Controllers.Admin;

[Route("api/admin/users")]

public sealed class UsersAdminController : BaseApiController
{
    private readonly IUserAdminService _service;

    public UsersAdminController(IUserAdminService service)
    {
        _service = service;
    }

    private async Task<IActionResult> HandleAsync(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [HttpGet]
    [Authorize(Permissions.Users.Read)]
    public async Task<IActionResult> GetUsers([FromQuery] AdminUsersFilterDto filter)
    {
        return await HandleAsync(async () =>
        {
            var data = await _service.GetUsersAsync(filter);
            return OkResponse(data, "USERS_RETURNED");
        });
    }
    [Authorize(Permissions.Users.Read)]
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserDetail([FromRoute] string id, [FromQuery] int ordersTake = 20)
    {
        return await HandleAsync(async () =>
        {
            var data = await _service.GetUserDetailAsync(id, ordersTake);
            return OkResponse(data, "USER_DETAILS_RETURNED");
        });
    }
}