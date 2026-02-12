using AutoMapper;
using MezuroApp.Application.Dtos.ProductCategory;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Mappings;

public class ProductCategoryMapping:Profile
    
{
    public  ProductCategoryMapping()
    {
        CreateMap<ProductCategory, ProductCategoryDto>().
            ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id)).
            ForMember(x=>x.ProductId, opt => opt.MapFrom(src=>src.ProductId)).
            ForMember(x=>x.CategoryId, opt => opt.MapFrom(src=>src.CategoryId));
    }
}