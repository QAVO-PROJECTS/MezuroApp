using AutoMapper;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Mappings
{
    public class AdminMappingProfile : Profile
    {
        public AdminMappingProfile()
        {
            CreateMap<AdminCreateRequestDto, Admin>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.Email)) // Identity üçün
                .ForMember(d => d.Username, opt => opt.MapFrom(s => s.Email)) // sənin custom property
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email))
                .ForMember(d => d.EmailConfirmed, opt => opt.MapFrom(_ => true)) // SuperAdmin tərəfindən yaradılırsa təsdiqli ola bilər
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest=>dest.PhoneNumber,opt=>opt.MapFrom(src=>src.PhoneNumber))
                ;

            CreateMap<User, AdminDto>()
                .ForMember(d => d.Roles, opt => opt.Ignore())
                .ForMember(d => d.Permissions, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    if (src is Admin admin)
                        dest.IsSuperAdmin = admin.IsSuperAdmin;
                });
        }
    }
}