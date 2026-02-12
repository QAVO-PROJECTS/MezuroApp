using AutoMapper;
using MezuroApp.Application.Dtos.ProductVariant;
using MezuroApp.Domain.Entities;

public class ProductVariantProfile : Profile
{
    public ProductVariantProfile()
    {
        // =============== ENTITY → DTO ===============

        // ProductVariant → ProductVariantDto
        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.ProductId, m => m.MapFrom(s => s.ProductId.ToString()))
            .ForMember(d => d.ProductColorId, m => m.MapFrom(s =>
                s.ProductColorId.HasValue ? s.ProductColorId.ToString() : null))
            .ForMember(d => d.OptionValues, m => m.MapFrom(s => s.OptionValues));

        // ProductVariantOptionValue → ProductVariantOptionValueDto
        CreateMap<ProductVariantOptionValue, ProductVariantOptionValueDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.OptionValueId, m => m.MapFrom(s => s.OptionValueId.ToString()))
            // OptionValue navigation-dan multi-language fields map
            .ForMember(d => d.ValueAz, m => m.MapFrom(s => s.OptionValue.ValueAz))
            .ForMember(d => d.ValueEn, m => m.MapFrom(s => s.OptionValue.ValueEn))
            .ForMember(d => d.ValueRu, m => m.MapFrom(s => s.OptionValue.ValueRu))
            .ForMember(d => d.ValueTr, m => m.MapFrom(s => s.OptionValue.ValueTr));


        // =============== DTO → ENTITY ===============

        // CreateProductVariantDto → ProductVariant
        CreateMap<CreateProductVariantDto, ProductVariant>()
            .ForMember(d => d.Id, m => m.Ignore())                         // service-də generate olunur
            .ForMember(d => d.ProductId, m => m.Ignore())                 // string → Guid service-də
            .ForMember(d => d.ProductColorId, m => m.Ignore())            // string → Guid?
            .ForMember(d => d.OptionValues, m => m.Ignore())              // upsert servisdə edilir
            .ForMember(d => d.VariantSlug, m => m.MapFrom(s => s.VariantSlug));

        // UpdateProductVariantDto → ProductVariant
        CreateMap<UpdateProductVariantDto, ProductVariant>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.ProductId, m => m.Ignore())
            .ForMember(d => d.ProductColorId, m => m.Ignore())
            .ForMember(d => d.OptionValues, m => m.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, val) => val != null));
    }
}