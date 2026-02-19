using AutoMapper;
using MezuroApp.Application.Dtos.Cupon;
using MezuroApp.Domain.Entities;
using System.Globalization;

public class CuponMappingProfile : Profile
{
    public CuponMappingProfile()
    {
        CreateMap<Cupon, CuponDto>()
            .ForMember(dest => dest.ValidFrom,
                opt => opt.MapFrom(src =>
                    src.ValidFrom.HasValue
                        ? src.ValidFrom.Value
                            .AddHours(4) // +4 saat
                            .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                        : null))
            .ForMember(dest => dest.ValidUntil,
                opt => opt.MapFrom(src =>
                    src.ValidUntil.HasValue
                        ? src.ValidUntil.Value
                            .AddHours(4) // +4 saat
                            .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                        : null))
            .ForMember(dest => dest.CreatedDate,
                opt => opt.MapFrom(src =>
                    src.CreatedDate
                        .AddHours(4) // +4 saat
                        .ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)));

        // Create & Update mappings
        CreateMap<CreateCuponDto, Cupon>();
        CreateMap<UpdateCuponDto, Cupon>();
    }
}