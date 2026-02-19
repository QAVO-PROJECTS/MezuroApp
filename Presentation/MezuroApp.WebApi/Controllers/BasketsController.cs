using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Basket.BasketItem;
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasketsController : BaseApiController
{
    private readonly IBasketService _service;

    public BasketsController(IBasketService service)
    {
        _service = service;
    }

    // ================================
    // 🔐 HELPERS (UserId yalnız Claims-dən)
    // ================================


   

    // ================================
    // 📌 GET (USER) — userId claims-dən
    // ================================
    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> GetUserBasket()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

      
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            var data = await _service.GetBasketForUserAsync(userId);
            return OkResponse(data, "BASKET_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // ================================
    // 📌 GET (GUEST) — footprintId frontdan gəlir
    // ================================
    [HttpGet("guest")]
    public async Task<IActionResult> GetGuestBasket([FromQuery] string footprintId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(footprintId))
                return BadRequestResponse("FOOTPRINT_REQUIRED");

            var data = await _service.GetBasketForGuestAsync(footprintId);
            return OkResponse(data, "BASKET_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // ================================
    // ➕ ADD / UPDATE (USER) — userId claims-dən
    // ================================
    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> AddOrUpdateUserBasket([FromBody] List<CreateBasketItemDto> items)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            await _service.AddOrUpdateBasketItemsForUserAsync(userId, items);
            return OkResponse<object>(null, "BASKET_UPDATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // ================================
    // ➕ ADD / UPDATE (GUEST) — footprintId frontdan
    // ================================
    [HttpPost("guest")]
    public async Task<IActionResult> AddOrUpdateGuestBasket(
        [FromQuery] string footprintId,
        [FromBody] List<CreateBasketItemDto> items)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(footprintId))
                return BadRequestResponse("FOOTPRINT_REQUIRED");

            await _service.AddOrUpdateBasketItemsForGuestAsync(footprintId, items);
            return OkResponse<object>(null, "BASKET_UPDATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // ================================
    // ❌ REMOVE ITEM (USER) — userId claims-dən
    // ================================
    [Authorize(Roles = "Customer")]
    [HttpDelete("user")]
    public async Task<IActionResult> RemoveUserBasketItem( [FromQuery]string productId, string? variantId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            await _service.RemoveBasketItemForUserAsync(userId, productId, variantId);
            return OkResponse<object>(null, "BASKET_ITEM_DELETED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // ================================
    // ❌ REMOVE ITEM (GUEST) — footprintId frontdan
    // ================================
    [HttpDelete("guest")]
    public async Task<IActionResult> RemoveGuestBasketItem(
        [FromQuery]
     string variantId,
     string productId,
     string footprintId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(footprintId))
                return BadRequestResponse("FOOTPRINT_REQUIRED");

            await _service.RemoveBasketItemForGuestAsync(footprintId, productId,variantId);
            return OkResponse<object>(null, "BASKET_ITEM_DELETED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    
    [HttpPost("merge")]
    [Authorize] // user-in login olması şərtdir
    public async Task<IActionResult> MergeGuestBasketIntoUser([FromQuery] string footprintId)
    {
        if (string.IsNullOrWhiteSpace(footprintId))
            return BadRequest("FOOTPRINT_REQUIRED");

        // UserId-i token-dən/HttpContext-dən alırıq
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub"); // OpenId Connect 'sub'

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        await _service.MergeBasketAsync(userId, footprintId);

        // İstəyə bağlı: merge-dən sonra footprint cookie-ni silmək
        // Response.Cookies.Delete("footprintId");

        return Ok(new { Message = "BASKET_MERGED" });
    }

    // ================================
    // 🗑 CLEAR USER BASKET — userId claims-dən
    // ================================
    [Authorize(Roles = "Customer")]
    [HttpDelete]
    public async Task<IActionResult> ClearUserBasket()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");

            await _service.RemoveAllBasketItemsForUserAsync(userId);
            return OkResponse<object>(null, "BASKET_CLEARED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}