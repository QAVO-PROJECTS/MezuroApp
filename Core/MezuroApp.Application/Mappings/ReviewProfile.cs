using AutoMapper;
using MezuroApp.Domain.Entities;
using MezuroApp.Application.Dtos.Review;
using System;

namespace MezuroApp.Application.Mappings;

public class ReviewProfile : Profile
{
    public ReviewProfile()
    {
        // Entity -> ReviewDto
        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Id,            m => m.MapFrom(s => s.Id.ToString()))
            .ForMember(d => d.ProductId,     m => m.MapFrom(s => s.ProductId.ToString()))
            .ForMember(d => d.UserId,     m => m.MapFrom(s => s.UserId.ToString()))
            .ForMember(d => d.UserName,      m => m.MapFrom(s => s.User != null ? /* s.User.FirstName */ s.User.FirstName : null))       // <-- öz property adınıza uyğunlaşdırın
            .ForMember(d => d.UserSurname,   m => m.MapFrom(s => s.User != null ? /* s.User.LastName */ s.User.LastName : null))     // <-- öz property adınıza uyğunlaşdırın
            .ForMember(d => d.UserProfileImage,
                                            m => m.MapFrom(s => s.User != null ? /* s.User.ProfileImage */ s.User.ProfileImage : null)) // <-- öz property adınıza uyğunlaşdırın
            .ForMember(d => d.AdminReplyDate,m => m.MapFrom(s => s.AdminReplyDate.HasValue ? s.AdminReplyDate.Value.AddDays(4).ToString("yyyy-MM-dd HH:mm:ss") : null))
            .ForMember(d => d.CreatedDate,   m => m.MapFrom(s => s.CreatedDate.AddHours(4).ToString("yyyy-MM-dd HH:mm:ss")))
            .ForMember(d => d.LikeCount,     m => m.MapFrom(s => s.LikeCount ?? 0))
            .ForMember(d => d.DislikeCount,  m => m.MapFrom(s => s.DislikeCount ?? 0))
            .ForMember(d => d.Status,        m => m.MapFrom(s => s.Status ?? true));

        // CreateReviewDto -> Entity
        CreateMap<CreateReviewDto, Review>()
            .ForMember(d => d.Id,                m => m.Ignore())
            .ForMember(d => d.ProductId,         m => m.MapFrom(s => Guid.Parse(s.ProductId)))
            .ForMember(d => d.UserId,
                opt => opt.MapFrom(s =>
                    string.IsNullOrWhiteSpace(s.UserId) ? (Guid?)null :
                        Guid.Parse(s.UserId)))
            .ForMember(d => d.AdminReplyDescription, m => m.Ignore())
            .ForMember(d => d.AdminReplyDate,    m => m.Ignore())
            .ForMember(d => d.LikeCount,         m => m.MapFrom(_ => 0))
            .ForMember(d => d.DislikeCount,      m => m.MapFrom(_ => 0))
            .ForMember(d => d.Status,            m => m.MapFrom(_ => true))
            .ForMember(d => d.CreatedDate,       m => m.Ignore())      // DbContext SaveChanges hook-da və ya default ilə
            .ForMember(d => d.LastUpdatedDate,   m => m.Ignore())
            .ForMember(d => d.IsDeleted,         m => m.MapFrom(_ => false));
    }
}
