using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductOption;

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
        return OkResponse(result, "OPTION_RETURNED");
    }

    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetByProduct(string productId)
    {
        var result = await _service.GetByProductAsync(productId);
        return OkResponse(result, "OPTIONS_BY_PRODUCT_RETURNED");
    }

    // ===================== CREATE =====================

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductOptionDto dto)
    {
        await _service.CreateAsync(dto);
        return CreatedResponse<object>(null,dto, "OPTION_CREATED");
    }

    // ===================== UPDATE =====================

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProductOptionDto dto)
    {
        await _service.UpdateAsync(dto);
        return OkResponse(dto,"OPTION_UPDATED");
    }

    // ===================== DELETE =====================

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return OkResponse(id,"OPTION_DELETED");
    }
}