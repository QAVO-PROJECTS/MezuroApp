using MezuroApp.Application.Dtos.Cupon;

namespace MezuroApp.Application.Abstracts.Services;

public interface ICuponService
{
    Task<List<CuponDto>> GetAllCupons();
    Task<List<CuponDto>> GetAllActiveCupons();
    Task<List<CuponDto>> GetAllInactiveCupons();
    Task<List<CuponDto>> PagedGetAllCupons(int pageNumber, int pageSize);
    Task<List<CuponDto>> PagedGetAllActiveCupons(int pageNumber, int pageSize);
    Task<List<CuponDto>> PagedGetAllInactiveCupons(int pageNumber, int pageSize);
    Task  CreateCupon(string adminId,CreateCuponDto createCuponDto);
    Task  UpdateCupon(UpdateCuponDto updateCuponDto);
    Task<CuponDto?> GetCuponById(string cuponId);
    Task <CuponDto?> GetCuponByCode(string cuponCode);
    Task DeleteCupon(string cuponId);
    Task SetActiveCupon(string cuponId,bool value);
}