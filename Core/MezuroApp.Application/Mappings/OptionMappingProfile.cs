using AutoMapper;
using MezuroApp.Application.Dtos.Option;

using MezuroApp.Domain.Entities;

public class OptionMappingProfile : Profile
{
    public OptionMappingProfile()
    {
        // Entity -> DTO

        CreateMap<Option, OptionDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            ;

        // Create DTO → Entity
        CreateMap<CreateOptionDto, Option>();
        // upsert-i servisdə edirik


        CreateMap<UpdateOptionDto, Option>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));



    }
}

