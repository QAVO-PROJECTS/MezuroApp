using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Payment;
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/user-cards")]
[Authorize(Roles = "Customer")]
public sealed class UserCardsController : BaseApiController
{
    private readonly IUserCardService _service;

    public UserCardsController(IUserCardService service)
    {
        _service = service;
    }

    // Mənim kartlarım (front burdan CardId götürəcək)
    [HttpGet("me")]
    public async Task<IActionResult> GetMyCards(CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var res = await _service.GetMyCardsAsync(userId!, ct);
            return OkResponse(res, "USER_CARDS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // Default kart seç
    [HttpPost("set-default")]
    public async Task<IActionResult> SetDefault([FromBody] SetDefaultCardDto dto, CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            await _service.SetDefaultAsync(userId!, dto.CardId, ct);
            return OkResponse(true, "USER_CARD_DEFAULT_SET");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}