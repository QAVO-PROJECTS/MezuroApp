using MailKit;
using MezuroApp.Application.Abstracts.Repositories;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Application.Abstracts.Repositories.OptionValues;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Repositories.ProductColorImages;
using MezuroApp.Application.Abstracts.Repositories.ProductColors;
using MezuroApp.Application.Abstracts.Repositories.ProductImages;
using MezuroApp.Application.Abstracts.Repositories.ProductOptions;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductVariantOptionValues;
using MezuroApp.Application.Abstracts.Repositories.ProductVariants;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Infrastructure.Concretes.Services;
using MezuroApp.Persistance.Concretes.Repositories;
using MezuroApp.Persistance.Concretes.Repositories.Categories;
using MezuroApp.Persistance.Concretes.Repositories.Options;
using MezuroApp.Persistance.Concretes.Repositories.OptionValues;
using MezuroApp.Persistance.Concretes.Repositories.ProductCategories;
using MezuroApp.Persistance.Concretes.Repositories.ProductColorImages;
using MezuroApp.Persistance.Concretes.Repositories.ProductColors;
using MezuroApp.Persistance.Concretes.Repositories.ProductImages;
using MezuroApp.Persistance.Concretes.Repositories.ProductOptions;
using MezuroApp.Persistance.Concretes.Repositories.Products;
using MezuroApp.Persistance.Concretes.Repositories.ProductVariantOptionValues;
using MezuroApp.Persistance.Concretes.Repositories.ProductVariants;
using MezuroApp.Persistance.Concretes.Services;
using Microsoft.Extensions.DependencyInjection;
using IMailService = MezuroApp.Application.Abstracts.Services.IMailService;
using MailService = MezuroApp.Infrastructure.Concretes.Services.MailService;


namespace MezuroApp.Persistance;

    public static class ServiceRegistration
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddSingleton<MongoDbContext>();
            //Repositories
            
            //Category
            services.AddScoped<ICategoryReadRepository, CategoryReadRepository>();
            services.AddScoped<ICategoryWriteRepository, CategoryWriteRepository>();
            //Product
            services.AddScoped<IProductReadRepository, ProductReadRepository>();
            services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            //ProductCategory
            services.AddScoped<IProductCategoryReadRepository,ProductCategoryReadRepository>();
            services.AddScoped<IProductCategoryWriteRepository,ProductCategoryWriteRepository>();
           //ColorImage
             services.AddScoped<IProductColorImageReadRepository,ProductColorImageReadRepository>();
             services.AddScoped<IProductColorImageWriteRepository,ProductColorImageWriteRepository>();
            //ProductImage
            services.AddScoped<IProductImageWriteRepository, ProductImageWriteRepository>();
            services.AddScoped<IProductImageReadRepository, ProductImageReadRepository>();
            //ProductVariant
            services.AddScoped<IProductVariantReadRepository, ProductVariantReadRepository>();
            services.AddScoped<IProductVariantWriteRepository, ProductVariantWriteRepository>();
            //ProductColor
            services.AddScoped<IProductColorReadRepository,ProductColorReadRepository>();
            services.AddScoped<IProductColorWriteRepository,ProductColorWriteRepository>();
            //ProductOption
            services.AddScoped<IProductOptionReadRepository, ProductOptionReadRepository>();
            services.AddScoped<IProductOptionWriteRepository, ProductOptionWriteRepository>();
            //ProductOptionValue
            services.AddScoped<IProductOptionValueReadRepository, ProductOptionValueReadRepository>();
            services.AddScoped<IProductOptionValueWriteRepository, ProductOptionValueWriteRepository>();
            //ProductVariantOptionValue
            services.AddScoped<IProductVariantOptionValueReadRepository,ProductVariantOptionValueReadRepository>();
            services.AddScoped<IProductVariantOptionValueWriteRepository,ProductVariantOptionValueWriteRepository>();
            
            //Option
            services.AddScoped<IOptionReadRepository, OptionReadRepository>();
            services.AddScoped<IOptionWriteRepository, OptionWriteRepository>();
            // Services
            services.AddScoped<IMailService,MailService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IUserAuthService, UserAuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IProductCategoryService, ProductCategoryService>();
            services.AddScoped<ICategoryService, CategoryService>();    
            services.AddScoped<IAuditLookupService, AuditLookupService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IProductColorService, ProductColorService>();
            services.AddScoped<IProductVariantService, ProductVariantService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IOptionService, OptionService>();
            services.AddScoped<IProductOptionService, ProductOptionService>();




        }
    }