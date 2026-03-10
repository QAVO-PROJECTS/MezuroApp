using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.EmailCampaigns;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EmailCampaignsController : BaseApiController
{
    private readonly IEmailCampaignService _service;

    public EmailCampaignsController(IEmailCampaignService service)
    {
        _service = service;
    }

    [Authorize(Permissions.EmailCampaigns.Read)]
    
    [HttpGet("estimate-recipients")]
    public async Task<IActionResult> Estimate([FromQuery] string targetSegment, CancellationToken ct)
    {
        try
        {
            var res = await _service.EstimateRecipientsAsync(targetSegment, ct);
            return OkResponse(res, "ESTIMATED_RECIPIENTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [Authorize(Permissions.EmailCampaigns.Update)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmailCampaignDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            var data = await _service.CreateAsync(userId, dto);
            return OkResponse(data, "CAMPAIGN_CREATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        // catch { return ServerErrorResponse(); }
    }



    [Authorize(Permissions.EmailCampaigns.Update)]
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            await _service.CancelAsync(userId, id);
            return OkResponse<object>(null, "CAMPAIGN_CANCELLED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [Authorize(Permissions.EmailCampaigns.Read)]
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllAsync();
            return OkResponse(data, "CAMPAIGNS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Permissions.EmailCampaigns.Read)]
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            return OkResponse(data, "CAMPAIGN_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}