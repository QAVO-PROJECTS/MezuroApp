using AutoMapper;
using MezuroApp.Application.Dtos.Order;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Mappings;

public class OrderProfile : Profile

{
    public OrderProfile()
    {
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.OrderItemId, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.ProductId, m => m.MapFrom(s => s.ProductId.ToString()))
            .ForMember(d => d.ProductVariantId, m => m.MapFrom(s => s.ProductVariantId.HasValue ? s.ProductVariantId.Value.ToString() : null))
            .ForMember(d => d.VariantName, m => m.MapFrom(s => s.ProductVariantName));

        CreateMap<Order, OrderDto>()
            .ForMember(d => d.OrderId, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.PreviewImages, m => m.MapFrom(s =>
                s.OrderItems
                    .Where(i => !i.IsDeleted && i.ProductImageUrl != null)
                    .OrderBy(i => i.CreatedDate)
                    .Select(i => i.ProductImageUrl!)
                    .Distinct()
                    .Take(4)
                    .ToList()
            )).ForMember(x=>x.CreatedDate, m => m.MapFrom(s=>s.CreatedDate.ToString("dd.MM.yyyy  HH:mm")));

        CreateMap<Order, OrderDetailDto>()
            .ForMember(d => d.OrderId, m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.Items, m => m.MapFrom(s => s.OrderItems.Where(i => !i.IsDeleted)))
            .ForMember(x=>x.CreatedDate, m => m.MapFrom(s => s.CreatedDate.ToString("dd.MM.yyyy  HH:mm")))
            .ForMember(x=>x.CouponCode,x=>x.MapFrom(x=>x.CuponCode));
    
        CreateMap<Order, OrderCreatedDto>()
            .ForMember(d => d.OrderId, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.Items, o => o.MapFrom(s => s.OrderItems));

        CreateMap<OrderItem, OrderItemCreatedDto>()
            .ForMember(d => d.OrderItemId, o => o.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId.ToString()))
            .ForMember(d => d.ProductVariantId, o => o.MapFrom(s => s.ProductVariantId.HasValue ? s.ProductVariantId.Value.ToString() : null));
    }
}