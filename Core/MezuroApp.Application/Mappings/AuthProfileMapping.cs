using System.Globalization;
using AutoMapper;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Application.Dtos.Auth;
using MezuroApp.Domain.Entities;
    
namespace MezuroApp.Application.Mappings
{
    public class AuthProfileMapping : Profile
    {
        public AuthProfileMapping()
        {
            // RegisterRequestDto-nu User modelinə çevirmək
            CreateMap<RegisterRequestDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email)) // Username olaraq email istifadə olunur
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.IsSubscribedToNewsletter, opt => opt.MapFrom(src => src.SubscribeToNewsletter))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Şifrə hash etməyi burada etməyəcəyik, o zaman şifrəni qarmaqarışıq saxlamağı təmin edəcəyik

            CreateMap<User, UserDto>();
            CreateMap<UpdateProfileDto, User>();
            CreateMap<User,ProfileDto>()
                .ForMember(dest => dest.BirthDate,
                    opt => opt.MapFrom(src =>
                        src.Birthday.HasValue
                            ? src.Birthday.Value
                                .AddHours(4) // +4 saat
                                .ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
                            : null)).ForMember(dest => dest.ProfileImageUrl,opt=>opt.MapFrom(u=>u.ProfileImage));
            // RegisterResponseDto-nu User və cavab məlumatlarına çevirmək
            CreateMap<User, RegisterResponseDto>()
                .ForMember(dest => dest.UserId,
                    opt => opt.MapFrom(src => true)) // Məlumatın uğurlu olduğunu qəbul edirik
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => true))
                // Məlumatın uğurlu olduğunu qəbul edirik
                .ForMember(dest => dest.EmailVerificationRequired,
                    opt => opt.MapFrom(src => true)); // Məlumatın uğurlu olduğunu qəbul edirik


        }
    }
}