using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Newsletter;
using MezuroApp.Application.Dtos.Newsletter.Admin;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : BaseApiController
{
    private readonly INewsletterService _service;

    public NewsletterController(INewsletterService service)
    {
        _service = service;
    }

    // ✅ Public subscribe (footer input)
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeNewsletterRequestDto dto)
    {
        try
        {
            var data = await _service.SubscribeAsync(dto);
            return OkResponse(data, "NEWSLETTER_SUBSCRIBED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // ✅ Public unsubscribe (email ilə)
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromQuery] string email, [FromQuery] string? reason)
    {
        try
        {
            await _service.UnsubscribeAsync(email, reason);
            return OkResponse<object>(null, "NEWSLETTER_UNSUBSCRIBED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [HttpGet]
    [Authorize(Permissions.EmailCampaigns.Read)]
    public async Task<IActionResult> Get([FromQuery] AdminNewsletterSubscribersFilterDto f, CancellationToken ct)
    {
        try
        {
            var data = await _service.GetAsync(f, ct);
            return OkResponse(data, "NEWSLETTER_SUBSCRIBERS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    // 🔐 Get current user's subscriber
    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            var data = await _service.GetMeAsync(userId);
            return OkResponse(data, "NEWSLETTER_ME_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // 🔐 Ensure subscriber for current user (sənin istədiyin endpoint)
    [Authorize(Roles = "Customer")]
    [HttpPost("me/ensure")]
    public async Task<IActionResult> EnsureMe([FromBody] EnsureSubscriberRequestDto? dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            var data = await _service.EnsureForCurrentUserAsync(userId, dto);
            return OkResponse(data, "NEWSLETTER_ENSURED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}