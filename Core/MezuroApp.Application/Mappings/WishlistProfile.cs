using AutoMapper;
using MezuroApp.Application.Dtos.Whislist;
using MezuroApp.Application.Dtos.WishlistItem;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Mappings;

public class WishlistProfile : Profile
{
    public WishlistProfile()
    {
        // Wishlist yaradılması
        CreateMap<CreateWishListDto, Wishlist>()
            .ForMember(dest => dest.Items, opt => opt.Ignore()); // Items sonradan əlavə ediləcək

        // WishlistItem yaradılması
        CreateMap<CreateWishlistItemDto, WishlistItem>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => Guid.Parse(src.ProductId)));

        // WishlistItem'dən DTO-ya (Product daxildir)
        CreateMap<WishlistItem, WishlistItemDto>()
            .ForMember(dest => dest.ProductDto, opt => opt.MapFrom(src => src.Product));

        // Wishlist-dən DTO-ya
        CreateMap<Wishlist, WishListDto>()
            .ForMember(dest => dest.WishlistItem, opt => opt.MapFrom(src => src.Items));
    }
}