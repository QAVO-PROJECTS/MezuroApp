using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Order;
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }

    [HttpPost("checkout")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateFromCheckout([FromBody] CreateOrderCheckoutDto dto)
    {
        try
        {
            var userId =
                User?.Identity?.IsAuthenticated == true
                    ? (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"))
                    : null;

            var created = await _service.CreateFromCheckoutAsync(userId, dto);

            return CreatedResponse($"/api/orders/{created.OrderId}", created, "ORDER_CREATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var data = await _service.GetMyOrdersAsync(userId!);
            return OkResponse(data, "ORDERS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [HttpGet("my/by-date")]
    [Authorize]
    public async Task<IActionResult> GetMyOrdersByDate([FromQuery] string dateFilter)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var data = await _service.GetMyOrdersByDateAsync(userId!, dateFilter);
            return OkResponse(data, "ORDERS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("change-status")]
    public async Task<IActionResult> ChangeStatus([FromQuery] string status, [FromQuery] string orderId)
    {
        try
        {
              await _service.SetOrderStatusAsync(orderId, status);
              return OkResponse(orderId, "ORDER_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }


    [HttpGet("my/by-status")]
    [Authorize]
    public async Task<IActionResult> GetMyOrdersByStatus([FromQuery] string status)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var data = await _service.GetMyOrdersByStatusAsync(userId!, status);
            return OkResponse(data, "ORDERS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }    [HttpGet("my/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetMyOrderDetail([FromRoute] string orderId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var data = await _service.GetOrderDetailAsync(userId!, orderId);
            return OkResponse(data, "ORDER_DETAIL_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }


}
