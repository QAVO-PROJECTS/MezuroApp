using AutoMapper;
using MezuroApp.Application.Dtos.ProductVariant;
using MezuroApp.Domain.Entities;

public class ProductVariantProfile : Profile
{
    public ProductVariantProfile()
    {
        CreateMap<ProductVariant, ProductVariantDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.Color, m => m.MapFrom(s => s.ProductColor))
            .ForMember(d => d.Product, m => m.MapFrom(s => s.Product))
            .ForMember(d => d.OptionValues, m => m.MapFrom(s => s.OptionValues))

            // ✅ Sənin istədiyin qayda
            .ForMember(d => d.Price, m => m.MapFrom(s => GetBaseProduct(s).Price + s.PriceModifier))
            .ForMember(d => d.CompareAtPrice, m => m.MapFrom(s =>
                (GetBaseProduct(s).CompareAtPrice ?? 0m) + s.CompareAtPriceModifier
            ));

        CreateMap<ProductVariantOptionValue, ProductVariantOptionValueDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.OptionValueId, m => m.MapFrom(s => s.OptionValueId.ToString()))
            .ForMember(d => d.ValueAz, m => m.MapFrom(s => s.OptionValue.ValueAz))
            .ForMember(d => d.ValueEn, m => m.MapFrom(s => s.OptionValue.ValueEn))
            .ForMember(d => d.ValueRu, m => m.MapFrom(s => s.OptionValue.ValueRu))
            .ForMember(d => d.ValueTr, m => m.MapFrom(s => s.OptionValue.ValueTr));

        CreateMap<CreateProductVariantDto, ProductVariant>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.ProductId, m => m.Ignore())
            .ForMember(d => d.ProductColorId, m => m.Ignore())
            .ForMember(d => d.OptionValues, m => m.Ignore())
            .ForMember(d => d.VariantSlug, m => m.MapFrom(s => s.VariantSlug));

        CreateMap<UpdateProductVariantDto, ProductVariant>()
            .ForMember(d => d.Id, m => m.Ignore())
            .ForMember(d => d.ProductId, m => m.Ignore())
            .ForMember(d => d.ProductColorId, m => m.Ignore())
            .ForMember(d => d.OptionValues, m => m.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, val) => val != null));
    }

    private static Product GetBaseProduct(ProductVariant v)
        => v.ProductColor?.Product ?? v.Product;
}
