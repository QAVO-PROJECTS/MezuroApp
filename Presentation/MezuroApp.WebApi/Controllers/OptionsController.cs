using Microsoft.AspNetCore.Mvc;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Option;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.HelperEntities;
using Microsoft.AspNetCore.Authorization;

namespace MezuroApp.WebApi.Controllers;

[Route("api/[controller]")]
public class OptionsController : BaseApiController
{
    private readonly IOptionService _service;

    public OptionsController(IOptionService service)
    {
        _service = service;
    }

    [Authorize(Policy = Permissions.Options.Read)]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] string id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            return OkResponse(result, "OPTION_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [HttpGet("search")]
    [Authorize(Policy = Permissions.Options.Read)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _service.SearchAsync(q, page, pageSize);
            return OkResponse(result, "OPTION_RETURNED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    
    [HttpGet]
    [Authorize(Policy = Permissions.Options.Read)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _service.GetAllAsync();
            return OkResponse(result, "OPTION_RETURNED");
        }   
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }


    [Authorize(Policy = Permissions.Options.Update)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOptionDto dto)
    {
        try
        {
            await _service.CreateAsync(dto);
            return CreatedResponse<object>(null, dto, "OPTION_CREATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [Authorize(Policy = Permissions.Options.Update)]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateOptionDto dto)
    {
        try
        {
            await _service.UpdateAsync(dto);
            return OkResponse<object>(dto, "OPTION_UPDATED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }

    [Authorize(Policy = Permissions.Options.Update)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] string id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return OkResponse<object>(null, "OPTION_DELETED");
        }
        catch (GlobalAppException ex) { return BadRequestResponse(ex.Message); }
        catch { return ServerErrorResponse(); }
    }
}