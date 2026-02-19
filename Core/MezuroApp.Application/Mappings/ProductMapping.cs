using AutoMapper;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Domain.Entities;
using System.Linq;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        // CREATE
        CreateMap<CreateProductDto, Product>()
            .ForMember(d => d.Images, opt => opt.Ignore())
            .ForMember(d => d.ProductCategories, opt => opt.Ignore())
            .ForMember(d => d.Variants, opt => opt.Ignore())
            .ForMember(d => d.ProductColors, opt => opt.Ignore())
            .ForAllMembers(o => o.Condition((src, dest, val) => val != null));

        // UPDATE
        CreateMap<UpdateProductDto, Product>()
            .ForMember(d => d.Images, opt => opt.Ignore())
            .ForMember(d => d.ProductCategories, opt => opt.Ignore())
            .ForMember(d => d.Variants, opt => opt.Ignore())
            .ForMember(d => d.ProductColors, opt => opt.Ignore())
            .ForAllMembers(o => o.Condition((src, dest, val) => val != null));

        // PRODUCT → DTO
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.RatingAverage, o => o.MapFrom(s => s.Reviews.Where(x=>x.Status==true).Sum(r => r.Rating) / s.Reviews.Count()))
            .ForMember(d=>d.ReviewCount, o=>o.MapFrom(s=>s.Reviews.Where(x=>x.Status==true).Count()))


            .ForMember(d => d.Categories, o => o.MapFrom(s =>
                s.ProductCategories
                    .Where(pc => !pc.IsDeleted)
                    .Select(pc => pc.Category)
            ))

            .ForMember(d => d.Options, o => o.MapFrom(s =>
                    s.Options.Where(o => !o.IsDeleted) // ProductOptionDto mapping edəcək
            ))

            .ForMember(d => d.Colors, o => o.MapFrom(s =>
                s.ProductColors.Where(pc => !pc.IsDeleted)
            ));

        CreateMap<ProductImage, ProductImageDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()));
    }
}