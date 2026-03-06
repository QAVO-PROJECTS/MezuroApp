using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductOption;
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
public class ProductOptionController : BaseApiController
{
    private readonly IProductOptionService _service;

    public ProductOptionController(IProductOptionService service)
    {
        _service = service;
    }

    // ===================== GET =====================

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        try
        {
            return OkResponse(result, "OPTION_RETURNED");
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

    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetByProduct(string productId)
    {
        var result = await _service.GetByProductAsync(productId);
      
        try
        {
            return OkResponse(result, "OPTIONS_BY_PRODUCT_RETURNED");
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

    // ===================== CREATE =====================
//
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductOptionDto dto)
    {
        await _service.CreateAsync(dto);

        try
        {
            return CreatedResponse<object>(null,dto, "OPTION_CREATED");
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

    // ===================== UPDATE =====================

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProductOptionDto dto)
    {
        await _service.UpdateAsync(dto);
      
        try
        {
            return OkResponse(dto, "OPTION_UPDATED");
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

    // ===================== DELETE =====================

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
     
        try
        {
            return OkResponse(id,"OPTION_DELETED");
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