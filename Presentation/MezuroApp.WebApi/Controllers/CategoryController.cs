using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Category;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Authorization;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
public class CategoryController : BaseApiController
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    /// <summary> Bütün kateqoriyaları qaytarır. </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllCategories();
            return OkResponse(data, "CATEGORIES_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message); // ex.Message lüğətdə açar deyilsə olduğu kimi çıxır
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }
    [HttpGet("active")]
    public async Task<IActionResult> GetAllActive()
    {
        try
        {
            var data = await _service.GetAllActiveCategories();
            return OkResponse(data, "CATEGORIES_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message); // ex.Message lüğətdə açar deyilsə olduğu kimi çıxır
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }
    [HttpGet("show-menu")]
    public async Task<IActionResult> GetAllMenuCategories()
    {
        try
        {
            var data = await _service.GetAllMenuCategories();
            return OkResponse(data, "CATEGORIES_RETURNED");
        }
        catch (GlobalAppException ex)
        {
            return BadRequestResponse(ex.Message); // ex.Message lüğətdə açar deyilsə olduğu kimi çıxır
        }
        catch (Exception)
        {
            return ServerErrorResponse();
        }
    }

    /// <summary> ParentId-ə görə kateqoriyaları qaytarır. </summary>
    [HttpGet("by-parent/{parentId}")]
    public async Task<IActionResult> GetByParentId([FromRoute] string parentId)
    {
        try
        {
            var data = await _service.GetAllCategoriesByParentId(parentId);
            return OkResponse(data, "CATEGORIES_RETURNED");
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
    [HttpGet("show-menu-by-parent/{parentId}")]
    public async Task<IActionResult> GetMenuCategoriesByParentId([FromRoute] string parentId)
    {
        try
        {
            var data = await _service.GetAllMenuCategoriesByParentId(parentId);
            return OkResponse(data, "CATEGORIES_RETURNED");
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

    [HttpPatch("{id}/active")]
    public async Task<IActionResult> SetIsActive([FromRoute] string id, [FromQuery] bool value)
    {
        try
        {
            await _service.SetIsActiveAsync(id, value);
            return OkResponse<object>(null, "CATEGORY_ACTIVE_STATUS_UPDATED");
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
    [HttpPatch("{id}/show-menu")]
    public async Task<IActionResult> SetIsShowMenu([FromRoute] string id, [FromQuery] bool value)
    {
        try
        {
            await _service.SetIsShowMenuAsync(id, value);
            return OkResponse<object>(null, "CATEGORY_SHOW_MENU_STATUS_UPDATED");
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
    /// <summary> Id-ə görə kateqoriya qaytarır. </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        try
        {
            var dto = await _service.GetCategoryById(id);
            if (dto is null)
                return NotFoundResponse("CATEGORY_NOT_FOUND");

            return OkResponse(dto, "CATEGORY_RETURNED");
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

    [Authorize(Policy = Permissions.Categories.Create)]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateCategoryDto dto)
    {
        try
        {
            await _service.CreateCategory(dto);
            return CreatedResponse<object>(null, dto, "CATEGORY_CREATED");
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
    // [Authorize(Policy = Permissions.Categories.Update)]
    /// <summary> Kateqoriyanı yeniləyir. </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromForm] UpdateCategoryDto dto)
    {
        try
        {
            await _service.UpdateCategory(dto);
            return OkResponse<object>(dto, "CATEGORY_UPDATED");
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

    // [Authorize(Policy = Permissions.Categories.Delete)]
    /// <summary> Id-ə görə kateqoriyanı silir (soft delete). </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            await _service.DeleteCategory(id);
            return OkResponse<object>(null, "CATEGORY_DELETED");
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
    // [Authorize(Policy = Permissions.Categories.Delete)]
    /// <summary> ParentId-ə görə bütün kateqoriyaları silir (soft delete). </summary>
    [HttpDelete("by-parent/{parentId}")]
    public async Task<IActionResult> DeleteByParent([FromRoute] string parentId)
    {
        try
        {
            await _service.DeleteAllCategoriesByParentId(parentId);
            return OkResponse<object>(null, "CATEGORIES_DELETED_BY_PARENT");
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
