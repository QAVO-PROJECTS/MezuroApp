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


  
    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetAll([FromRoute]string productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var data = await _service.GetAllByProductAsync(productId, page, pageSize);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("by-product-admin-inactive/{productId}")]
    public async Task<IActionResult> GetAllInActiveByProduct([FromRoute]string productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var data = await _service.GetAllInactiveByProductForAdminAsync(productId, page, pageSize);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("by-product-admin/{productId}")]
    public async Task<IActionResult> GetAllActiveByProduct([FromRoute]string productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var data = await _service.GetAllActiveByProductForAdminAsync(productId, page, pageSize);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("by-rating-admin-inactive/{rating}")]
    public async Task<IActionResult> GetAllInActiveByRating([FromRoute]int rating, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var data = await _service.GetAllInactiveByRatingForAdminAsync( rating, page, pageSize);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("by-rating-admin/{rating}")]
    public async Task<IActionResult> GetAllActiveByRating([FromRoute]int rating, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var data = await _service.GetAllActiveByRatingForAdminAsync( rating, page, pageSize);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("inactive")]
    public async Task<IActionResult> GetAllInActive([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var data = await _service.GetAllInActiveAsync( page, pageSize);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAllForAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var data = await _service.GetAllForAdminAsync( page, pageSize);
            return OkResponse(data, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("sorted-inactive")]
    public async Task<IActionResult> GetSortedInactive(
        [FromQuery] int sort = 2,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _service.GetAllInActiveSortedAsync(sort, page, pageSize);
            return OkResponse(result, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch
        {
            return ServerErrorResponse();
        }
    }
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("by-status")]
    public async Task<IActionResult> GetByStatus(
        [FromQuery] bool value,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _service.GetByStatusAndDeleteAsync(value, page, pageSize);
            return OkResponse(result, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch
        {
            return ServerErrorResponse();
        }
    }
    
    [Authorize(Policy = Permissions.Reviews.Read)]
    [HttpGet("sorted-all")]
    public async Task<IActionResult> GetSortedActive(
        [FromQuery] int sort = 2,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _service.GetAllActiveSortedAsync(sort, page, pageSize);
            return OkResponse(result, "REVIEWS_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch
        {
            return ServerErrorResponse();
        }
    }

    [Authorize(Policy = Permissions.Reviews.Update)]
   
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

    [Authorize(Policy = Permissions.Reviews.Update)]
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

    [Authorize(Policy = Permissions.Reviews.Update)]
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

 
    [Authorize(Roles="Customer")]
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

    [Authorize(Roles="Customer")]
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


    [Authorize(Policy = Permissions.Reviews.Update)]
    [HttpDelete("reject/{id}")]
    public async Task<IActionResult> Reject([FromRoute] string id)
    {
        try
        {
            await _service.RejectAsync(id);
            return OkResponse<object>(null, "REVIEW_DELETED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}
