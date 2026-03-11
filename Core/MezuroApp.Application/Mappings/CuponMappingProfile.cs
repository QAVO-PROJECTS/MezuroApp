using AutoMapper;
using MezuroApp.Application.Dtos.Cupon;
using MezuroApp.Domain.Entities;
using System.Globalization;
using MezuroApp.Application.GlobalException;

public class CuponMappingProfile : Profile
{
    public CuponMappingProfile()
    {
        CreateMap<Cupon, CuponDto>()
            .ForMember(dest => dest.DiscountType,
                opt => opt.MapFrom(src =>(src.DiscountType)))
            .ForMember(dest => dest.ValidFrom,
                opt => opt.MapFrom(src =>
                    src.ValidFrom.HasValue
                        ? src.ValidFrom.Value.AddHours(4).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                        : null))
            .ForMember(dest => dest.ValidUntil,
                opt => opt.MapFrom(src =>
                    src.ValidUntil.HasValue
                        ? src.ValidUntil.Value.AddHours(4).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                        : null))
            .ForMember(dest => dest.CreatedDate,
                opt => opt.MapFrom(src =>
                    src.CreatedDate.AddHours(4).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)));

        CreateMap<CreateCuponDto, Cupon>()
            .ForMember(dest => dest.ValidFrom, opt => opt.Ignore())
            .ForMember(dest => dest.ValidUntil, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountType, opt => opt.Ignore());

        CreateMap<UpdateCuponDto, Cupon>()
            .ForMember(dest => dest.ValidFrom, opt => opt.Ignore())
            .ForMember(dest => dest.ValidUntil, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountType, opt => opt.Ignore());
    }

}