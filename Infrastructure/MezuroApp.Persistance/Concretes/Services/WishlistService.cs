using AutoMapper;
using MezuroApp.Application.Abstracts.Repositories.WishlistItems;
using MezuroApp.Application.Abstracts.Repositories.Wishlists;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.WishlistItem;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MezuroApp.Persistance.Concretes.Services;

public class WishlistService : IWishlistService
{
    private readonly IWishlistReadRepository _wishlistReadRepo;
    private readonly IWishlistWriteRepository _wishlistWriteRepo;
    private readonly IWishlistItemWriteRepository _wishlistItemWriteRepo;
    private readonly IMapper _mapper;

    public WishlistService(
        IWishlistReadRepository wishlistReadRepo,
        IWishlistWriteRepository wishlistWriteRepo,
        IMapper mapper,
        IWishlistItemWriteRepository wishlistItemWriteRepo)
    {
        _wishlistReadRepo = wishlistReadRepo;
        _wishlistWriteRepo = wishlistWriteRepo;
        _mapper = mapper;
        _wishlistItemWriteRepo = wishlistItemWriteRepo;
    }

    // 2. GET USER WISHLIST ITEMS
    public async Task<List<WishlistItemDto>> GetUserWishlistItemsAsync(string userId)
    {
        var guserId = Guid.TryParse(userId, out var userIdGuid) ? userIdGuid : Guid.Empty;

        var wishlist = await _wishlistReadRepo.GetAsync(
            w => w.UserId == guserId,
            q => q.Include(x => x.Items.Where(i => !i.IsDeleted && !i.Product.IsDeleted))
                  .ThenInclude(i => i.Product)
                      .ThenInclude(p => p.Images)
                .Include(x => x.Items.Where(i => !i.IsDeleted && !i.Product.IsDeleted))
                  .ThenInclude(i => i.Product)
                      .ThenInclude(p => p.ProductCategories)
                .Include(x => x.Items.Where(i => !i.IsDeleted && !i.Product.IsDeleted))
                  .ThenInclude(i => i.Product)
                      .ThenInclude(p => p.ProductColors.Where(pc => !pc.IsDeleted))
                          .ThenInclude(pc => pc.ColorImages)
                .Include(x => x.Items.Where(i => !i.IsDeleted && !i.Product.IsDeleted))
                  .ThenInclude(i => i.Product)
                      .ThenInclude(p => p.Options.Where(o => !o.IsDeleted))
                          .ThenInclude(o => o.Option)
                .Include(x => x.Items.Where(i => !i.IsDeleted && !i.Product.IsDeleted))
                  .ThenInclude(i => i.Product)
                      .ThenInclude(p => p.Options.Where(o => !o.IsDeleted))
                          .ThenInclude(o => o.Values.Where(v => !v.IsDeleted))
                .Include(x => x.Items.Where(i => !i.IsDeleted && !i.Product.IsDeleted))
                  .ThenInclude(i => i.Product)
                      .ThenInclude(p => p.ProductColors)
                          .ThenInclude(pc => pc.ColorImages)
                              .ThenInclude(ci => ci.ProductImage)
                .Include(x => x.Items.Where(i => !i.IsDeleted && !i.Product.IsDeleted))
                  .ThenInclude(i => i.Product)
                      .ThenInclude(p => p.Reviews)
        );

        if (wishlist == null) return new();

        return _mapper.Map<List<WishlistItemDto>>(wishlist.Items);
    }

    public async Task ManageWishlistItemsAsync(string userId, string? productId = null, List<CreateWishlistItemDto>? products = null)
    {
        var guserId = Guid.TryParse(userId, out var userIdGuid) ? userIdGuid : Guid.Empty;

        var wishlist = await _wishlistReadRepo.GetAsync(
            w => w.UserId == guserId,
            q => q.Include(w => w.Items)
        );

        // Əgər mövcud deyilsə, yarat
        if (wishlist == null)
        {
            wishlist = new Wishlist
            {
                Id = Guid.NewGuid(),
                UserId = guserId,
                Items = new List<WishlistItem>()
            };

            await _wishlistWriteRepo.AddAsync(wishlist);
            await _wishlistWriteRepo.CommitAsync(); // Tracking üçün
        }

        // 1) Tək məhsul üçün toggle
        if (!string.IsNullOrWhiteSpace(productId))
        {
            if (!Guid.TryParse(productId, out var pid))
                throw new GlobalAppException("PRODUCT_ID_NOT_FOUND");

            var existing = wishlist.Items.FirstOrDefault(i => i.ProductId == pid);
            if (existing != null)
            {
                await _wishlistItemWriteRepo.HardDeleteAsync(existing);
            }
            else
            {
                var newItem = new WishlistItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = pid,
                    WishlistId = wishlist.Id
                };
                await _wishlistItemWriteRepo.AddAsync(newItem);
            }
        }

        // 2) Çoxlu məhsul üçün merge (mövcud olmayanları əlavə et)
        if (products is { Count: > 0 })
        {
            var existingProductIds = wishlist.Items.Select(i => i.ProductId).ToHashSet();

            foreach (var product in products)
            {
                if (Guid.TryParse(product.ProductId, out var parsedId) && !existingProductIds.Contains(parsedId))
                {
                    var newItem = new WishlistItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = parsedId,
                        WishlistId = wishlist.Id
                    };

                    await _wishlistItemWriteRepo.AddAsync(newItem);
                }
            }
        }

        await _wishlistWriteRepo.CommitAsync();
    }
}
