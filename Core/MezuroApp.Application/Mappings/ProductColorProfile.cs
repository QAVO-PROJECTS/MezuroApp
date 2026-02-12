using AutoMapper;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.ProductColor;
using MezuroApp.Domain.Entities;

public class ProductColorProfile : Profile
{
    public ProductColorProfile()
    {
        // CREATE → ENTITY
        CreateMap<CreateProductColorDto, ProductColor>()
            .ForMember(d => d.ProductId, opt => opt.Ignore())
            .ForMember(d => d.ColorImages, opt => opt.Ignore())
            .ForAllMembers(o => o.Condition((src, dest, val) => val != null));

        // UPDATE → ENTITY
        CreateMap<UpdateProductColorDto, ProductColor>()
            .ForMember(d => d.ColorImages, opt => opt.Ignore())
            .ForAllMembers(o => o.Condition((src, dest, val) => val != null));

        // Short DTO
        CreateMap<ProductColor, ProductColorShortDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.ColorImages, opt => opt.MapFrom(s =>
                s.ColorImages
                    .Where(ci => !ci.IsDeleted && ci.ProductImage != null && !ci.ProductImage.IsDeleted)
                    .Select(ci => ci.ProductImage)
            ));

        // Full DTO
        CreateMap<ProductColor, ProductColorDto>()
            .ForMember(d => d.Id, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.ColorImages, opt => opt.MapFrom(s =>
                s.ColorImages
                    .Where(ci => !ci.IsDeleted && ci.ProductImage != null && !ci.ProductImage.IsDeleted)
                    .Select(ci => ci.ProductImage)
            ))
            .ForMember(d => d.ColorVariants, opt => opt.MapFrom(s => s.ColorVariants));

        // ProductImage → DTO
        CreateMap<ProductImage, ProductImageDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id.ToString()));
    }
}