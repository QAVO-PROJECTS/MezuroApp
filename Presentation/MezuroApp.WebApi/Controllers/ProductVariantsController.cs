using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.ProductVariant;
using MezuroApp.Application.GlobalException;

namespace MezuroApp.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ProductVariantController : BaseApiController
    {
        private readonly IProductVariantService _service;

        public ProductVariantController(IProductVariantService service)
        {
            _service = service;
        }

        // ===============================================
        //                   GET BY ID
        // ===============================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return OkResponse(result, "PRODUCT_VARIANT_RETURNED");
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

        // ===============================================
        //         GET ALL VARIANTS OF A PRODUCT
        // ===============================================
        [HttpGet("by-product/{productId}")]
        public async Task<IActionResult> GetByProduct(string productId)
        {
            try
            {
                var variants = await _service.GetByProductAsync(productId);
                return OkResponse(variants, "PRODUCT_VARIANTS_RETURNED");
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

        // ===============================================
        //                   CREATE
        // ===============================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductVariantDto dto)
        {
            try
            {
                await _service.CreateAsync(dto);
                return CreatedResponse<object>(null, dto, "PRODUCT_VARIANT_CREATED");
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

        // ===============================================
        //                   UPDATE
        // ===============================================
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProductVariantDto dto)
        {
            try
            {
                await _service.UpdateAsync(dto);
                return OkResponse(dto, "PRODUCT_VARIANT_UPDATED");
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

        // ===============================================
        //                   DELETE
        // ===============================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return OkResponse(id, "PRODUCT_VARIANT_DELETED");
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
}