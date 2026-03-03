using MailKit;
using MezuroApp.Application.Abstracts.Repositories;
using MezuroApp.Application.Abstracts.Repositories.AbandonedCarts;
using MezuroApp.Application.Abstracts.Repositories.Addresses;
using MezuroApp.Application.Abstracts.Repositories.BasketItems;
using MezuroApp.Application.Abstracts.Repositories.Baskets;
using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Repositories.Cupons;
using MezuroApp.Application.Abstracts.Repositories.EmailCampaignLogs;
using MezuroApp.Application.Abstracts.Repositories.EmailCampaigns;
using MezuroApp.Application.Abstracts.Repositories.NewsletterSubscribers;
using MezuroApp.Application.Abstracts.Repositories.Options;
using MezuroApp.Application.Abstracts.Repositories.OptionValues;
using MezuroApp.Application.Abstracts.Repositories.OrderItems;
using MezuroApp.Application.Abstracts.Repositories.Orders;
using MezuroApp.Application.Abstracts.Repositories.PaymentTransactions;
using MezuroApp.Application.Abstracts.Repositories.ProductCategories;
using MezuroApp.Application.Abstracts.Repositories.ProductColorImages;
using MezuroApp.Application.Abstracts.Repositories.ProductColors;
using MezuroApp.Application.Abstracts.Repositories.ProductImages;
using MezuroApp.Application.Abstracts.Repositories.ProductOptions;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Repositories.ProductVariantOptionValues;
using MezuroApp.Application.Abstracts.Repositories.ProductVariants;
using MezuroApp.Application.Abstracts.Repositories.Reviews;
using MezuroApp.Application.Abstracts.Repositories.UserCards;
using MezuroApp.Application.Abstracts.Repositories.WishlistItems;
using MezuroApp.Application.Abstracts.Repositories.Wishlists;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Infrastructure.Concretes.Services;
using MezuroApp.Persistance.Concretes.BackgroundServices;
using MezuroApp.Persistance.Concretes.Repositories;
using MezuroApp.Persistance.Concretes.Repositories.AbandonedCarts;
using MezuroApp.Persistance.Concretes.Repositories.Addresses;
using MezuroApp.Persistance.Concretes.Repositories.BasketItems;
using MezuroApp.Persistance.Concretes.Repositories.Baskets;
using MezuroApp.Persistance.Concretes.Repositories.Categories;
using MezuroApp.Persistance.Concretes.Repositories.Cupons;
using MezuroApp.Persistance.Concretes.Repositories.EmailCampaignLogs;
using MezuroApp.Persistance.Concretes.Repositories.EmailCampaigns;
using MezuroApp.Persistance.Concretes.Repositories.NewsletterSubscribers;
using MezuroApp.Persistance.Concretes.Repositories.Options;
using MezuroApp.Persistance.Concretes.Repositories.OptionValues;
using MezuroApp.Persistance.Concretes.Repositories.OrderItems;
using MezuroApp.Persistance.Concretes.Repositories.Orders;
using MezuroApp.Persistance.Concretes.Repositories.PaymentTransactions;
using MezuroApp.Persistance.Concretes.Repositories.ProductCategories;
using MezuroApp.Persistance.Concretes.Repositories.ProductColorImages;
using MezuroApp.Persistance.Concretes.Repositories.ProductColors;
using MezuroApp.Persistance.Concretes.Repositories.ProductImages;
using MezuroApp.Persistance.Concretes.Repositories.ProductOptions;
using MezuroApp.Persistance.Concretes.Repositories.Products;
using MezuroApp.Persistance.Concretes.Repositories.ProductVariantOptionValues;
using MezuroApp.Persistance.Concretes.Repositories.ProductVariants;
using MezuroApp.Persistance.Concretes.Repositories.Reviews;
using MezuroApp.Persistance.Concretes.Repositories.UserCards;
using MezuroApp.Persistance.Concretes.Repositories.WishlistItems;
using MezuroApp.Persistance.Concretes.Repositories.Wishlists;
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
            //Review
            services.AddScoped<IReviewReadRepository, ReviewReadRepository>();
            services.AddScoped<IReviewWriteRepository, ReviewWriteRepository>();
            //Wishlist
            services.AddScoped<IWishlistReadRepository,WishlistReadRepository>();
            services.AddScoped<IWishlistWriteRepository,WishlistWriteRepository>();
            //WishlistItem
            services.AddScoped<IWishlistItemReadRepository,WishlistItemReadRepository>();
            services.AddScoped<IWishlistItemWriteRepository,WishlistItemWriteRepository>();
            //BasketItem
            services.AddScoped<IBasketItemWriteRepository, BasketItemWriteRepository>();
            services.AddScoped<IBasketItemReadRepository, BasketItemReadRepository>();
            //Basket
            services.AddScoped<IBasketWriteRepository, BasketWriteRepository>();
            services.AddScoped<IBasketReadRepository, BasketReadRepository>();
            //Cupon
            services.AddScoped<ICuponReadRepository, CuponReadRepository>();
            services.AddScoped<ICuponWriteRepository, CuponWriteRepository>();
            //Address
            services.AddScoped<IAddressReadRepository, AddressReadRepository>();
            services.AddScoped<IAddressWriteRepository, AddressWriteRepository>();
            //Order
            services.AddScoped<IOrderReadRepository, OrderReadRepository>();
            services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
            //OrderItem
            services.AddScoped<IOrderItemReadRepository, OrderItemReadRepository>();
            services.AddScoped<IOrderItemWriteRepository, OrderItemWriteRepository>();
            //PaymentTransaction
            services.AddScoped<IPaymentTransactionReadRepository, PaymentTransactionReadRepository>();
            services.AddScoped<IPaymentTransactionWriteRepository, PaymentTransactionWriteRepository>();
            //AbandonedCart
            services.AddScoped<IAbandonedCartReadRepository,AbandonedCartReadRepository>();
            services.AddScoped<IAbandonedCartWriteRepository, AbandonedCartWriteRepository>();
            //NewsletterSubscriber
    

            services.AddScoped<INewsletterSubscriberReadRepository, NewsletterSubscriberReadRepository>();
            services.AddScoped<INewsletterSubscriberWriteRepository, NewsletterSubscriberWriteRepository>();
            //EmailCampaign
            services.AddScoped<IEmailCampaignWriteRepository, EmailCampaignWriteRepository>();
            services.AddScoped<IEmailCampaignReadRepository, EmailCampaignReadRepository>();
            //EmailCampaign
            services.AddScoped<IEmailCampaignLogReadRepository, EmailCampaignLogReadRepository>();
            services.AddScoped<IEmailCampaignLogWriteRepository, EmailCampaignLogWriteRepository>();
            //UserCard
            services.AddScoped<IUserCardReadRepository, UserCardReadRepository>();
            services.AddScoped<IUserCardWriteRepository, UserCardWriteRepository>();
            
            
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
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<IWishlistService, WishlistService>();
            services.AddScoped<IBasketService, BasketService>();
            services.AddScoped<ICuponService, CuponService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<INewsletterService, NewsletterService>();
            services.AddScoped<IEmailCampaignService, EmailCampaignService>();
            services.AddScoped<IAbandonedCartAdminService, AbandonedCartAdminService>();
            services.AddScoped<IUserAdminService, UserAdminService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IUserCardService, UserCardService>(); 
            services.AddScoped<IAdminOrderService, AdminOrderService>();
            services.AddScoped<IAdminRefundService, AdminRefundService>();
            services.AddScoped<IAdminTransactionService, AdminTransactionService>();
            services.AddScoped<AdminAuditLogService>();
            
            //Background Services
            services.AddHostedService<AbandonedCartBackgroundService>();
            services.AddHostedService<EmailCampaignSenderBackgroundService>();
            services.AddHttpContextAccessor();
       






        }
    }