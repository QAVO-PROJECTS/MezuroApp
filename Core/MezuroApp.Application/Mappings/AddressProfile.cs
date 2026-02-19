using AutoMapper;
using MezuroApp.Application.Dtos.Auth.Adress;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Mappings
{
    public class AddressProfile : Profile
    {
        public AddressProfile()
        {
            // Entity -> DTO
            CreateMap<UserAddress, AdressDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                ;

            // Create DTO -> Entity
            CreateMap<CreateAddressDto,UserAddress>();

            // Update DTO -> Entity (null gələn sahələr overwrite etməsin)
            CreateMap<UpdateAdressDto,UserAddress>()
                .ForAllMembers(opt =>
                    opt.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}