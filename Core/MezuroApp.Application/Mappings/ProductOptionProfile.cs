using AutoMapper;
using MezuroApp.Application.Dtos.ProductOption;
using MezuroApp.Domain.Entities;

public class ProductOptionProfile : Profile
{
    public ProductOptionProfile()
    {
        // ProductOption -> DTO
        CreateMap<ProductOption, ProductOptionDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.ProductId, m => m.MapFrom(s => s.ProductId.ToString()))
            .ForMember(d => d.OptionNameAz, m => m.MapFrom(s => s.Option.NameAz))
            .ForMember(d => d.OptionNameEn, m => m.MapFrom(s => s.Option.NameEn))
            .ForMember(d => d.OptionNameTr, m => m.MapFrom(s => s.Option.NameTr))
            .ForMember(d => d.OptionNameRu, m => m.MapFrom(s => s.Option.NameRu))
            
            .ForMember(d => d.Values, m => m.MapFrom(s => s.Values));

        // ProductOptionValue -> DTO
        CreateMap<ProductOptionValue, UpdateProductOptionValueDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()));
   

        // CreateProductOptionDto -> ProductOption (Value-lar servisdə idarə olunur)
        CreateMap<CreateProductOptionDto, ProductOption>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.ProductId, m => m.Ignore())
            .ForMember(d => d.OptionId, m => m.Ignore())
            .ForMember(d => d.Values, m => m.Ignore());

        // UpdateProductOptionDto -> ProductOption
        CreateMap<UpdateProductOptionDto, ProductOption>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.OptionId, m => m.Ignore())
            .ForMember(d => d.ProductId, m => m.Ignore())
            .ForMember(d => d.Values, m => m.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, val) => val != null));

        // UpdateProductOptionValueDto -> ProductOptionValue
        CreateMap<UpdateProductOptionValueDto, ProductOptionValue>()
            .ForAllMembers(opt => opt.Condition((src, dest, val) => val != null));
    }
}