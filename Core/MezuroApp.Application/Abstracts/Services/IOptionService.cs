using MezuroApp.Application.Dtos.Option;
using MezuroApp.Application.Dtos.Review;
using MezuroApp.Domain.HelperEntities;


public interface IOptionService
{
    Task<OptionDto> GetByIdAsync(string id);
    Task<List<OptionDto>> GetAllAsync();
    Task CreateAsync(CreateOptionDto dto);
    Task UpdateAsync(UpdateOptionDto dto);
    Task DeleteAsync(string id);
    Task<PagedResult<OptionDto>> SearchAsync(string? query, int pageNumber, int pageSize);


}

