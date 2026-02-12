using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductColor;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Authorization;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
public class ProductColorsController : BaseApiController
{
    private readonly IProductColorService _service;

    public ProductColorsController(IProductColorService service)
    {
        _service = service;
    }

    /// <summary> Id-ə görə product color qaytarır </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id);
            return OkResponse(dto, "PRODUCT_COLOR_RETURNED");
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

    /// <summary> ProductId-ə görə bütün product color-ları qaytarır </summary>
    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetAll([FromRoute] string productId)
    {
        try
        {
            var data = await _service.GetAllAsync(productId);
            return OkResponse(data, "PRODUCT_COLORS_RETURNED");
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

    // [Authorize(Policy = Permissions.ProductColors.Create)]
    /// <summary> Yeni product color yaradır </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductColorDto dto)
    {
        try
        {
            await _service.CreateAsync(dto);
            return CreatedResponse<object>(null, dto, "PRODUCT_COLOR_CREATED");
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
    // [Authorize(Policy = Permissions.ProductColors.Update)]

    /// <summary> Product color yeniləyir </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProductColorDto dto)
    {
        try
        {
            await _service.UpdateAsync(dto);
            return OkResponse<object>(dto, "PRODUCT_COLOR_UPDATED");
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

    // [Authorize(Policy = Permissions.ProductColors.Delete)]
    /// <summary> Product color silir (soft delete) </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return OkResponse<object>(null, "PRODUCT_COLOR_DELETED");
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
}
