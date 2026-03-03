using System.Globalization;
using AutoMapper;
using MezuroApp.Application.Dtos.Admins;
using MezuroApp.Application.Dtos.Auth;
using MezuroApp.Domain.Entities;
    
namespace MezuroApp.Application.Mappings
{
    public class AuthProfileMapping : Profile
    {
        private static readonly string DateFmt = "dd.MM.yyyy HH:mm:ss";
        private static readonly TimeZoneInfo BakuTz = ResolveBakuTimeZone();
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

            CreateMap<User, UserDto>().
                ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.ProfileImage))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.LastLoginDate, opt => opt.MapFrom(src =>FormatLocal(src.LastLoginAt?? src.CreatedAt))
                );
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
        private static string? FormatLocal(DateTime value)
        {
            if (value == DateTime.MinValue) return null;

            var utc = value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);

            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, BakuTz);
            return local.ToString(DateFmt);
        }

        private static TimeZoneInfo ResolveBakuTimeZone()
        {
            try
            {
                // Linux/macOS
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Baku");
            }
            catch
            {
                try
                {
                    // Windows
                    return TimeZoneInfo.FindSystemTimeZoneById("Azerbaijan Standard Time");
                }
                catch
                {
                    return TimeZoneInfo.Local;
                }
            }
        }
    }
}