using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Authorization;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
public class ProductsController : BaseApiController
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    /// <summary> Bütün məhsulları qaytarır </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllAsync();
            return OkResponse(data, "PRODUCTS_RETURNED");
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
    [HttpGet("by-category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(
        [FromRoute] string categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetByCategoryAsync(categoryId, page, pageSize);
            return OkResponse(result, "PRODUCTS_BY_CATEGORY_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    /// <summary> Id-ə görə məhsulu qaytarır </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        try
        {
            var dto = await _service.GetByIdAsync(id);
            if (dto is null)
                return NotFoundResponse("PRODUCT_NOT_FOUND");

            return OkResponse(dto, "PRODUCT_RETURNED");
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

    // [Authorize(Policy = Permissions.Products.Create)]
    /// <summary> Yeni məhsul yaradır </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateProductDto dto)
    {
        try
        {
            await _service.CreateAsync(dto);
            return CreatedResponse<object>(null, dto, "PRODUCT_CREATED");
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

    // [Authorize(Policy = Permissions.Products.Update)]
    /// <summary> Məhsulu yeniləyir </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromForm] UpdateProductDto dto)
    {
        try
        {
            await _service.UpdateAsync(dto);
            return OkResponse<object>(dto, "PRODUCT_UPDATED");
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

    // [Authorize(Policy = Permissions.Products.Delete)]
    /// <summary> Məhsulu Id-ə görə silir (soft delete) </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return OkResponse<object>(null, "PRODUCT_DELETED");
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

    // -------------------------------------------------------
    // STATUS METHODS (toggle)
    // -------------------------------------------------------

    // [Authorize(Policy = Permissions.Products.SetActive)]
    /// <summary> Məhsulun IsActive statusunu dəyişir </summary>
    [HttpPatch("{id}/active")]
    public async Task<IActionResult> SetIsActive([FromRoute] string id, [FromQuery] bool value)
    {
        try
        {
            await _service.SetIsActiveAsync(id, value);
            return OkResponse<object>(null, "PRODUCT_ACTIVE_STATUS_UPDATED");
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

    
    [Authorize(Policy = Permissions.Products.SetFeatured)]

    /// <summary> Məhsulun IsFeatured statusunu dəyişir </summary>
    [HttpPatch("{id}/featured")]
    public async Task<IActionResult> SetIsFeatured([FromRoute] string id, [FromQuery] bool value)
    {
        try
        {
            await _service.SetIsFeaturedAsync(id, value);
            return OkResponse<object>(null, "PRODUCT_FEATURED_STATUS_UPDATED");
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

    
    [Authorize(Policy = Permissions.Products.SetNew)]

    /// <summary> Məhsulun IsNew statusunu dəyişir </summary>
    [HttpPatch("{id}/new")]
    public async Task<IActionResult> SetIsNew([FromRoute] string id, [FromQuery] bool value)
    {
        try
        {
            await _service.SetIsNewAsync(id, value);
            return OkResponse<object>(null, "PRODUCT_NEW_STATUS_UPDATED");
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

    [Authorize(Policy = Permissions.Products.SetSale)]
    /// <summary> Məhsulun IsOnSale statusunu dəyişir </summary>
    [HttpPatch("{id}/onsale")]
    public async Task<IActionResult> SetIsOnSale([FromRoute] string id, [FromQuery] bool value)
    {
        try
        {
            await _service.SetIsOnSaleAsync(id, value);
            return OkResponse<object>(null, "PRODUCT_SALE_STATUS_UPDATED");
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