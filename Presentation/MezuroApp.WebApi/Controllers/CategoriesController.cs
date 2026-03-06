using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Category;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Authorization;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _service;

    public CategoriesController(ICategoryService service)
    {
        _service = service;
    }

    /// <summary> Bütün kateqoriyaları qaytarır. </summary>
    [Authorize(Policy = Permissions.Categories.Read)]
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
    [Authorize(Policy = Permissions.Categories.Read)]
    [HttpGet("filtered-activated-status")]
    public async Task<IActionResult> GetAllFilteredStatus(bool isActive)
    {
        try
        {
            var data = await _service.GetFilteredCategoriesForActiveStatus(isActive);
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
    [Authorize(Policy = Permissions.Categories.Read)]
    [HttpGet("filtered-show-menu")]
    public async Task<IActionResult> GetAllFilteredShowMenuStatus(bool isShowMenu)
    {
        try
        {
            var data = await _service.GetFilteredCategoriesForShowMenu(isShowMenu);
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
    [Authorize(Policy = Permissions.Categories.Read)]
    [HttpGet("sub-categories-filtered-activated-status")]
    public async Task<IActionResult> GetAllSubCategoriesForFilteredStatus(string parentId,bool isActive)
    {
        try
        {
            var data = await _service.GetFilteredSubCategoriesForActiveStatus(parentId,isActive);
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
    [Authorize(Policy = Permissions.Categories.Read)]
    [HttpGet("sub-categories-filtered-show-menu-status")]
    public async Task<IActionResult> GetAllSubCategoriesForFilteredShowMenuStatus(string parentId,bool isShowMenu)
    {
        try
        {
            var data = await _service.GetFilteredSubCategoriesForShowMenu(parentId,isShowMenu);
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
    // For User
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
    //For User
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
     //For User 

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
    //For User
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
    [Authorize(Policy = Permissions.Categories.Update)]

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
    [Authorize(Policy = Permissions.Categories.Update)]
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
    //For User
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
    [Authorize(Policy = Permissions.Categories.Read)]
    [HttpGet("admin/{id}")]
    public async Task<IActionResult> GetByIdForAdmin([FromRoute] string id)
    {
        try
        {
            var dto = await _service.GetCategoryByIdForAdmin(id);
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

    [Authorize(Policy = Permissions.Categories.Update)]
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

     [Authorize(Policy = Permissions.Categories.Update)]
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

 
    [Authorize(Policy = Permissions.Categories.Update)]
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

    [Authorize(Policy = Permissions.Categories.Update)]
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
