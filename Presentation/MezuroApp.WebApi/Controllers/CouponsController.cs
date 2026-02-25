using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Cupon;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities; // Permissions
using MezuroApp.WebApi.Controllers;

namespace MezuroApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : BaseApiController
    {
        private readonly ICuponService _service;

        public CouponsController(ICuponService service)
        {
            _service = service;
        }

        // ================================
        // 📌 LISTS
        // ================================
        [HttpGet]
        [Authorize(Policy = Permissions.Coupons.Read)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _service.GetAllCupons();
                return OkResponse(data, "COUPONS_RETURNED");
            }
            catch (GlobalAppException ex)
            {
                return BadRequestResponse(ex.Message);
            }
           
        }

        [HttpGet("filter-active")]
        [Authorize(Policy = Permissions.Coupons.Read)]
        public async Task<IActionResult> GetAllActive(  string? validFrom,
            string? validUntil,bool isActive, int pageNumber, int pageSize)
        {
            try
            {
                var data = await _service.GetAllFilterCupons(validFrom,validUntil,isActive, pageNumber, pageSize);
                return OkResponse(data, "COUPONS_RETURNED");
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

  

        [HttpGet("paged")]
        [Authorize(Policy = Permissions.Coupons.Read)]
        public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string? filter = null)
        {
            try
            {
                pageNumber = pageNumber <= 0 ? 1 : pageNumber;
                pageSize = pageSize <= 0 ? 20 : pageSize;

                var data = filter?.ToLower() switch
                {
                    "active" => await _service.PagedGetAllActiveCupons(pageNumber, pageSize),
                    "inactive" => await _service.PagedGetAllInactiveCupons(pageNumber, pageSize),
                    _ => await _service.PagedGetAllCupons(pageNumber, pageSize)
                };

                return OkResponse(data, "COUPONS_RETURNED");
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

        // ================================
        // 📌 GET BY ID / CODE
        // ================================
        [HttpGet("{id}")]
        [Authorize(Policy = Permissions.Coupons.Read)]
        public async Task<IActionResult> GetById([FromRoute] string id)
        {
            try
            {
                var data = await _service.GetCuponById(id);
                if (data == null) return NotFoundResponse("NOT_FOUND_CUPON");
                return OkResponse(data, "COUPON_RETURNED");
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

        [HttpGet("by-code/{code}")]
        [Authorize(Policy = Permissions.Coupons.Read)]
        public async Task<IActionResult> GetByCode([FromRoute] string code)
        {
            try
            {
                var data = await _service.GetCuponByCode(code);
                if (data == null) return NotFoundResponse("NOT_FOUND_CUPON");
                return OkResponse(data, "COUPON_RETURNED");
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

        // ================================
        // ➕ CREATE
        // ================================
        [HttpPost]
         [Authorize(Policy = Permissions.Coupons.Update)]
        public async Task<IActionResult> Create([FromBody] CreateCuponDto dto)
        {
            try
            {
                // AdminId-ni **login olmuş admin**-dən götürürük (frontdan gələn dəyəri override edirik)
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");
                if (string.IsNullOrWhiteSpace(adminId))
                    return BadRequestResponse("USER_ID_NOT_FOUND");

           

                await _service.CreateCupon(adminId,dto);

                // Location üçün yeni yaradılanı code ilə çəkə bilərik
                var created = await _service.GetCuponByCode(dto.Code);
                var location = created is not null ? $"api/coupons/{created.Id}" : null;

                return CreatedResponse(location, created, "COUPON_CREATED");
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

        // ================================
        // ✏️ UPDATE
        // ================================
        [HttpPut]
        [Authorize(Policy = Permissions.Coupons.Update)]
        public async Task<IActionResult> Update([FromBody] UpdateCuponDto dto)
        {
            try
            {
                await _service.UpdateCupon(dto);
                // yenisini qaytara bilərik (istəyə görə)
                var updated = await _service.GetCuponById(dto.Id);
                return OkResponse(updated, "COUPON_UPDATED");
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

        // ================================
        // 🟢/🔴 SET ACTIVE
   

        [Authorize(Policy = Permissions.Coupons.Update)]
        [HttpPatch("{id}/isactive")]
        public async Task<IActionResult> IsActive([FromRoute] string id, [FromQuery] bool value)
        {
            try
            {
                await _service.SetActiveCupon(id, value);
                return OkResponse<object>(null, "COUPON_ACTIVE_STATUS_UPDATED");
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

        // ================================
        // ❌ DELETE
        // ================================
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.Coupons.Update)]
        public async Task<IActionResult> Delete([FromRoute] string id)
        {
            try
            {
                await _service.DeleteCupon(id);
                return OkResponse<object>(null, "COUPON_DELETED");
            }
            catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
            catch { return ServerErrorResponse(); }
        }
    }
}