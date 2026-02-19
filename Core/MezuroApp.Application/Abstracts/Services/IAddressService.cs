using MezuroApp.Application.Dtos.Auth.Adress;

namespace MezuroApp.Application.Abstracts.Services;

public interface IAddressService
{
    Task <List<AdressDto>>GetAllAddressesAsync(string userId);
    Task<AdressDto> GetAddressByIdAsync(string userId,string id);
    Task CreateAddressAsync(string userId,CreateAddressDto address);
    Task UpdateAddressAsync(string userId,UpdateAdressDto address);
    Task DeleteAddressAsync(string userId,string id);
    
}