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
            .ForMember(d => d.Id, opt => opt.Ignore())    
            .ForMember(d => d.Images, opt => opt.Ignore())
            .ForMember(d => d.ProductCategories, opt => opt.Ignore())
            .ForMember(d => d.Variants, opt => opt.Ignore())
            .ForMember(d => d.ProductColors, opt => opt.Ignore())
            .ForAllMembers(o => o.Condition((src, dest, val) => val != null));

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.RatingAverage, o => o.Ignore())
            .ForMember(d => d.ReviewCount, o => o.Ignore())
            .AfterMap((src, dest) =>
            {
                var approved = (src.Reviews ?? new List<Review>()).Where(r => r.Status == true).ToList();
                dest.ReviewCount = approved.Count;
                dest.RatingAverage = dest.ReviewCount == 0 ? 0m : approved.Sum(r => (decimal)r.Rating) / dest.ReviewCount;
            })
            .ForMember(d => d.Categories, o => o.MapFrom(s =>
                s.ProductCategories
                    .Where(pc => !pc.IsDeleted)
                    .Select(pc => pc.Category)
            ))
            .ForMember(d => d.Options, o => o.MapFrom(s =>
                s.Options.Where(o => !o.IsDeleted)
            ))
            .ForMember(d => d.Colors, o => o.MapFrom(s =>
                s.ProductColors.Where(pc => !pc.IsDeleted)
            ));

        CreateMap<ProductImage, ProductImageDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.ToString()));
    }
}