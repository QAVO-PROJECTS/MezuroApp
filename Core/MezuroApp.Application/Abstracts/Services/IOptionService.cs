using MezuroApp.Application.Dtos.Option;


public interface IOptionService
{
    Task<OptionDto> GetByIdAsync(string id);
    Task<List<OptionDto>> GetAllAsync();
    Task CreateAsync(CreateOptionDto dto);
    Task UpdateAsync(UpdateOptionDto dto);
    Task DeleteAsync(string id);
}

