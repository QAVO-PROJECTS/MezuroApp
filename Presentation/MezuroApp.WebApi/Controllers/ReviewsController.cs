using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Review;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Authorization;

// using Microsoft.AspNetCore.Authorization; // istəyə görə aç

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
public class ReviewsController : BaseApiController
{
    private readonly IReviewService _service;

    public ReviewsController(IReviewService service)
    {
        _service = service;
    }

    /// <summary>Id-ə görə rəy qaytarır.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            return OkResponse(data, "REVIEW_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

     // [Authorize(Policy = Permissions.Reviews.GetAll)]
    /// <summary>Məhsula aid bütün rəyləri qaytarır.</summary>
    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetAll([FromRoute] string productId)
    {
        try
        {
            var data = await _service.GetAllAsync(productId);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    /// <summary>Məhsula aid aktiv (Status=true) rəyləri qaytarır.</summary>
    [HttpGet("by-product/{productId}/active")]
    public async Task<IActionResult> GetAllActive([FromRoute] string productId)
    {
        try
        {
            var data = await _service.GetAllActiveAsync(productId);
            return OkResponse(data, "REVIEWS_ACTIVE_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // ============================================================
    // CREATE
    // ============================================================

    /// <summary>Yeni rəy yaradır.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        try
        {
    
            await _service.CreateAsync(dto);
     
            return CreatedResponse<object>(null, dto, "REVIEW_CREATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    /// <summary>Rəyə admin cavabı əlavə edir.</summary>
    [Authorize(Policy = Permissions.Reviews.Reply)]
    [HttpPatch("reply")]
    public async Task<IActionResult> Reply([FromBody] ReplyReviewDto dto)
    {
        try
        {
            if (dto == null) return BadRequestResponse("INVALID_INPUT");
            
            await _service.ReplyAsync(dto);
            return OkResponse<object>(null, "REVIEW_REPLIED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    /// <summary>Rəyin statusunu dəyişir.</summary>
     [Authorize(Policy = Permissions.Reviews.SetStatus)]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> EditStatus([FromRoute] string id, [FromQuery] bool status)
    {
        try
        {
            await _service.EditStatusAsync(id, status);
            return OkResponse<object>(null, "REVIEW_STATUS_UPDATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    /// <summary>Rəyi bəyən (like count +1).</summary>
    [HttpPatch("{id}/like")]
    public async Task<IActionResult> Like([FromRoute] string id)
    {
        try
        {
            await _service.InCreaseAsync(id);
            return OkResponse<object>(null, "REVIEW_LIKED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    /// <summary>Rəyi bəyənmə (dislike count +1).</summary>
    [HttpPatch("{id}/dislike")]
    public async Task<IActionResult> Dislike([FromRoute] string id)
    {
        try
        {
            await _service.DeCrease(id);
            return OkResponse<object>(null, "REVIEW_DISLIKED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    /// <summary>Rəyləri sıralayır (Default/Newest/Oldest/Most/Least helpful/Highest/Lowest rating).</summary>
    [HttpPost("sort")]
    public async Task<IActionResult> Sort([FromBody] SortReviewDto dto)
    {
        try
        {
            var data = await _service.SortReview(dto);
            return OkResponse(data, "REVIEWS_SORTED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    /// <summary>Rəyi silir (soft delete).</summary>
    [Authorize(Policy = Permissions.Reviews.Delete)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return OkResponse<object>(null, "REVIEW_DELETED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}
