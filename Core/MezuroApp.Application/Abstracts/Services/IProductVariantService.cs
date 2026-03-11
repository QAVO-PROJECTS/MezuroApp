using MezuroApp.Application.Dtos.ProductVariant;

namespace MezuroApp.Application.Abstracts.Services
{
    public interface IProductVariantService
    {
        // ================= READ =================

        /// <summary>
        /// Variantı Id ilə qaytarır.
        /// </summary>
        Task<ProductVariantDto> GetByIdAsync(string id);

         /// <summary>
        /// Variantı Slug ilə qaytarır.
        /// </summary>
        Task<ProductVariantDto> GetBySlugAsync(string slug);

        /// <summary>
        /// Bir məhsula aid bütün variantları qaytarır.
        /// </summary>
        Task<List<ProductVariantDto>> GetByProductAsync(string productId);



        // ================= CREATE =================

        /// <summary>
        /// Yeni variant yaradır:
        /// - SKU unik kontrol
        /// - Slug null gələrsə avtomatik generasiya
        /// - OptionValue-lar variantla əlaqələndirilir
        /// - Product.StockQuantity null-dursa variant stock-ları toplanıb yazılır
        /// </summary>
        Task CreateAsync(CreateProductVariantDto dto);



        // ================= UPDATE =================

        /// <summary>
        /// Mövcud variantı yeniləyir:
        /// - SKU unik kontrol
        /// - Slug null gələrsə təkrar generasiya
        /// - OptionValue-lar upsert edilir
        /// - Product.StockQuantity null-dursa yenidən hesablanır
        /// </summary>
        Task UpdateAsync(UpdateProductVariantDto dto);



        // ================= DELETE =================

        /// <summary>
        /// Variantı silir (soft delete).
        /// Silindikdən sonra əgər Product.StockQuantity null-dursa,
        /// variantların toplam stokuna əsasən product stock yenilənir.
        /// </summary>
        Task DeleteAsync(string id);
    }
}
