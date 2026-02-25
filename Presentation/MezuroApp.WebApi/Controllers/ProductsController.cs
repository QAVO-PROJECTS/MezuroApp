using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.Product.ProductFilter;
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
    [Authorize(Policy = Permissions.Products.Read)]
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
    [HttpGet("best-seller")]
    public async Task<IActionResult> GetAllBestSeller(

        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetAllBestSellerAsync(page, pageSize);
            return OkResponse(result, "PRODUCTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [HttpGet("on-sale")]
    public async Task<IActionResult> GetAllOnSale(
 
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetAllOnSaleAsync(page, pageSize);
            return OkResponse(result, "PRODUCTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [HttpGet("newest")]
    public async Task<IActionResult> GetAllNewest(
  
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetAllNewProductAsync(page, pageSize);
            return OkResponse(result, "PRODUCTS_RETURNED");
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
    // ✅ 1) FILTER META (colors + counts, options + values)
// GET: /api/products/filter-meta?categoryId=... (optional)
    [HttpGet("filter-meta")]
    public async Task<IActionResult> GetFilterMeta([FromQuery] string? categoryId = null,[FromQuery] string lang = "az")
    {
        try
        {
            var result = await _service.GetFilterMetaAsync(categoryId, lang);
            return OkResponse(result, "PRODUCT_FILTER_META_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
         catch { return ServerErrorResponse(); }
    }

// ✅ 2) FILTER PRODUCTS
// POST: /api/products/filter
    [HttpPost("filter")]
    public async Task<IActionResult> Filter([FromBody] ProductFilterRequestDto request)
    {
        try
        {
            var result = await _service.FilterAsync(request);
            return OkResponse(result, "PRODUCTS_FILTERED_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [Authorize(Policy = Permissions.Products.Read)]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAllProductForAdmin([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.GetAllProductForAdminAsync(page, pageSize);
            return OkResponse(result, "PRODUCTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Products.Read)]
    [HttpGet("admin/search")]
    public async Task<IActionResult> AdminSearch(
        [FromQuery] string term,
        [FromQuery] string lang = "az",
     
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.AdminSearchAsync(term, lang,  page, pageSize);
            return OkResponse(result, "PRODUCTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    [Authorize(Policy = Permissions.Products.Read)]
    [HttpGet("admin/filter-status")]
    public async Task<IActionResult> GetAllStatusFilteredProductForAdmin([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,[FromQuery]bool status=true)
    {
        try
        {
            var result = await _service.GetAllStatusFilteredProductForAdminAsync(page, pageSize,status);
            return OkResponse(result, "PRODUCTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
    // [Authorize(Policy = Permissions.Products.Read)]
    [HttpGet("admin/filter-by-price")]
    public async Task<IActionResult> AdminFilterByPrice([FromQuery] AdminProductFilterRequestDto r)
    {
        try
        {
            var result = await _service.AdminFilterByPriceAsync(r);
            return OkResponse(result, "PRODUCTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    // [Authorize(Policy = Permissions.Products.Read)]
    [HttpGet("admin/sorted")]
    public async Task<IActionResult> AdminSorted([FromQuery] AdminProductSortRequestDto r)
    {
        try
        {
            var result = await _service.AdminSortedAsync(r);
            return OkResponse(result, "PRODUCTS_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [Authorize(Policy = Permissions.Products.Update)]
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

    [Authorize(Policy = Permissions.Products.Update)]
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

     [Authorize(Policy = Permissions.Products.Update)]
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


    [Authorize(Policy = Permissions.Products.Update)]
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
    [Authorize(Policy = Permissions.Products.Update)]
    [HttpPatch("{id}/bestseller")]
    public async Task<IActionResult> SetIsBestSeller([FromRoute] string id, [FromQuery] bool value)
    {
        try
        {
            await _service.SetIsBestSellerAsync(id, value);
            return OkResponse<object>(null, "PRODUCT_BESTSELLER_STATUS_UPDATED");
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

    
    [Authorize(Policy = Permissions.Products.Update)]


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

    
    [Authorize(Policy = Permissions.Products.Update)]


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

    [Authorize(Policy = Permissions.Products.Update)]

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