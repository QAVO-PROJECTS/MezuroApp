using System.Security.Claims;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.WishlistItem;
using MezuroApp.Application.GlobalException;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WishlistController : BaseApiController
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    // İstəyin: Customer rolu (istəyinə görə "User" da ola bilər)
    [Authorize(Roles = "Customer")]
    [HttpPost("manage")]
    public async Task<IActionResult> ManageWishlist([FromBody] List<CreateWishlistItemDto>? products)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { StatusCode = 401, Error = LocalizeAll("USER_ID_NOT_FOUND") });

            if (products == null || !products.Any())
                return BadRequestResponse("EMPTY_PRODUCT_LIST");

            if (products.Count == 1)
            {
                // Tək məhsul üçün toggle
                await _wishlistService.ManageWishlistItemsAsync(userId, productId: products[0].ProductId);
            }
            else
            {
                // Çox məhsul üçün merge
                await _wishlistService.ManageWishlistItemsAsync(userId, products: products);
            }

            return OkResponse<object>(products, "WISHLIST_UPDATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // İstəyin: Customer rolu (əvvəl "User" idi; istəsən geri qaytara bilərəm)
    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> GetUserWishlist()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { StatusCode = 401, Error = LocalizeAll("USER_ID_NOT_FOUND") });

            var items = await _wishlistService.GetUserWishlistItemsAsync(userId);
            return OkResponse(items, "WISHLIST_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}