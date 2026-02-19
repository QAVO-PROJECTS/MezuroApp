using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Auth.Adress;
using MezuroApp.Application.GlobalException;
using Microsoft.AspNetCore.Authorization;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AddressesController : BaseApiController
{
    private readonly IAddressService _service;

    public AddressesController(IAddressService service)
    {
        _service = service;
    }

    [AllowAnonymous]
    /// <summary> İstifadəçinin bütün ünvanlarını qaytarır </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");
            var data = await _service.GetAllAddressesAsync(userId);
            return OkResponse(data, "ADDRESSES_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }
    [AllowAnonymous]
    /// <summary> ID-yə görə ünvan qaytarır </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");
            var data = await _service.GetAddressByIdAsync(userId, id);
            return OkResponse(data, "ADDRESS_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }

    /// <summary> Yeni ünvan yaradır </summary>
     [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAddressDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");
            await _service.CreateAddressAsync(userId, dto);
            return CreatedResponse<object>(null, dto, "ADDRESS_CREATED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }
    [AllowAnonymous]
    /// <summary> Ünvanı yeniləyir </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateAdressDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");
            await _service.UpdateAddressAsync(userId, dto);
            return OkResponse<object>(dto, "ADDRESS_UPDATED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }

    /// <summary> Ünvanı silir (soft delete) </summary>
    [AllowAnonymous]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequestResponse("USER_ID_NOT_FOUND");
            await _service.DeleteAddressAsync(userId, id);
            return OkResponse<object>(null, "ADDRESS_DELETED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }
}
