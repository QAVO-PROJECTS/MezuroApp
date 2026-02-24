using System;
using System.Linq;
using AutoMapper;
using MezuroApp.Application.Dtos.Category;
using MezuroApp.Domain.Entities;

namespace MezuroApp.Application.Mappings
{
    public class CategoryMapping : Profile
    {
        private static readonly string DateFmt = "dd.MM.yyyy HH:mm:ss";
        private static readonly TimeZoneInfo BakuTz = ResolveBakuTimeZone();

        public CategoryMapping()
        {
            // ===== Category -> CategoryShortDto =====
            CreateMap<Category, CategoryShortDto>()
                .ForMember(d => d.Id,       opt => opt.MapFrom(s => s.Id.ToString()))
                .ForMember(d => d.ParentId, opt => opt.MapFrom(s => s.ParentId.HasValue ? s.ParentId.Value.ToString() : null))
                // 🔽 Bakı saatı + saniyə ilə format
                .ForMember(d => d.CreatedDate,     opt => opt.MapFrom(s => FormatLocal(s.CreatedDate)))
                .ForMember(d => d.LastUpdatedDate, opt => opt.MapFrom(s => FormatLocal(s.LastUpdatedDate)))
                .ForMember(d => d.DeletedDate,     opt => opt.MapFrom(s => s.IsDeleted ? FormatLocal(s.DeletedDate) : null))
                // Relations
               ;

            // ===== Category -> CategoryDto (tam) =====
            CreateMap<Category, CategoryDto>()
                .ForMember(d => d.Id,       opt => opt.MapFrom(s => s.Id.ToString()))
                .ForMember(d => d.ParentId, opt => opt.MapFrom(s => s.ParentId.HasValue ? s.ParentId.Value.ToString() : null))
                .ForMember(d => d.Categories, opt => opt.MapFrom(s => s.Children))
                .ForMember(d => d.CreatedDate,     opt => opt.MapFrom(s => FormatLocal(s.CreatedDate)))
                .ForMember(d => d.LastUpdatedDate, opt => opt.MapFrom(s => FormatLocal(s.LastUpdatedDate)))
                .ForMember(d => d.DeletedDate,     opt => opt.MapFrom(s => s.IsDeleted ? FormatLocal(s.DeletedDate) : null))
               ;

            // ===== CategoryShortDto -> Category =====
            CreateMap<CategoryShortDto, Category>()
                .ForMember(d => d.Id,       opt => opt.MapFrom(s => Guid.Parse(s.Id)))
                .ForMember(d => d.ParentId, opt => opt.MapFrom(s =>
                    string.IsNullOrWhiteSpace(s.ParentId) ? (Guid?)null : Guid.Parse(s.ParentId)))
                .ForMember(d => d.Children, opt => opt.Ignore())
                .ForMember(d => d.ProductCategories, opt => opt.Ignore());

            // ===== CreateCategoryDto -> Category =====
            CreateMap<CreateCategoryDto, Category>()
                .ForMember(d => d.ParentId, opt => opt.MapFrom(s =>
                    string.IsNullOrWhiteSpace(s.ParentId) ? (Guid?)null : Guid.Parse(s.ParentId)))
                .ForMember(d => d.ImageUrl,  opt => opt.Ignore())

                .ForMember(d => d.ProductCategories, opt => opt.Ignore())
                .ForMember(d => d.Children,  opt => opt.Ignore());

            // ===== UpdateCategoryDto -> Category (partial update) =====
            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(d => d.Id,       opt => opt.MapFrom(s => Guid.Parse(s.Id)))
                .ForMember(d => d.ParentId, opt => opt.MapFrom(s =>
                    string.IsNullOrWhiteSpace(s.ParentId) ? (Guid?)null : Guid.Parse(s.ParentId)))
                .ForMember(d => d.ImageUrl, opt => opt.Ignore())
                .ForMember(d => d.ProductCategories, opt => opt.Ignore())
                .ForMember(d => d.Children, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        }

        // ===== Helpers =====

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