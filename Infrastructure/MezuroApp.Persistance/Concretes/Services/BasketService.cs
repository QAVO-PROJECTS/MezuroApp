using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Basket;
using MezuroApp.Application.Dtos.Basket.BasketItem;
using MezuroApp.Application.Dtos.Product;
using MezuroApp.Application.Dtos.ProductVariant;
using MezuroApp.Application.GlobalException;
using MezuroApp.Domain.Entities;

using MezuroApp.Application.Abstracts.Repositories.Baskets;
using MezuroApp.Application.Abstracts.Repositories.BasketItems;

public class BasketService : IBasketService
{
    private readonly IBasketReadRepository _basketRead;
    private readonly IBasketWriteRepository _basketWrite;
    private readonly IBasketItemReadRepository _basketItemRead;
    private readonly IBasketItemWriteRepository _basketItemWrite;
    private readonly IMapper _mapper;

    public BasketService(
        IBasketReadRepository basketRead,
        IBasketWriteRepository basketWrite,
        IBasketItemReadRepository basketItemRead,
        IBasketItemWriteRepository basketItemWrite,
        IMapper mapper)
    {
        _basketRead = basketRead;
        _basketWrite = basketWrite;
        _basketItemRead = basketItemRead;
        _basketItemWrite = basketItemWrite;
        _mapper = mapper;
    }

    // ======================================================
    //               ADD / UPDATE (User)
    // ======================================================
    public async Task AddOrUpdateBasketItemsForUserAsync(string userId, List<CreateBasketItemDto> itemsToAdd)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        if (itemsToAdd == null || itemsToAdd.Count == 0)
            return;

        var normalized = NormalizeIncomingItems(itemsToAdd);

        var basket = await _basketRead.GetAsync(
            b => b.UserId == uid && !b.IsDeleted,
            q => q.Include(x => x.BasketItems),
            enableTracking: true
        );

        if (basket == null)
        {
            basket = new Basket
            {
                Id = Guid.NewGuid(),
                UserId = uid,
                CreatedDate = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow,
                IsDeleted = false,
                BasketItems = new List<BasketItem>()
            };

            await _basketWrite.AddAsync(basket);
            await _basketWrite.CommitAsync();
        }

        foreach (var kvp in normalized)
        {
            var (productId, variantId) = kvp.Key;
            var qty = kvp.Value;

            var existingActive = basket.BasketItems?
                .FirstOrDefault(bi => bi.ProductId == productId &&
                                      bi.ProductVariantId == variantId &&
                                      !bi.IsDeleted);

            var existingAny = basket.BasketItems?
                .FirstOrDefault(bi => bi.ProductId == productId &&
                                      bi.ProductVariantId == variantId);

            // REMOVE
            if (qty <= 0)
            {
                if (existingActive != null)
                {
                    existingActive.IsDeleted = true;
                    existingActive.DeletedDate = DateTime.UtcNow;
                    existingActive.LastUpdatedDate = DateTime.UtcNow;
                    await _basketItemWrite.UpdateAsync(existingActive);
                }
                continue;
            }

            // INSERT
            if (existingAny == null)
            {
                var newItem = new BasketItem
                {
                    Id = Guid.NewGuid(),
                    BasketId = basket.Id,
                    ProductId = productId,
                    ProductVariantId = variantId,
                    Quantity = qty,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _basketItemWrite.AddAsync(newItem);
                basket.BasketItems!.Add(newItem);
            }
            else
            {
                if (existingAny.IsDeleted)
                {
                    existingAny.IsDeleted = false;
             
                }

                existingAny.Quantity = qty; // idempotent set
                existingAny.LastUpdatedDate = DateTime.UtcNow;
                await _basketItemWrite.UpdateAsync(existingAny);
            }
        }

        basket.LastUpdatedDate = DateTime.UtcNow;
        await _basketWrite.UpdateAsync(basket);
        await _basketWrite.CommitAsync();
    }

    // ======================================================
    //               ADD / UPDATE (Guest)
    // ======================================================
    public async Task AddOrUpdateBasketItemsForGuestAsync(string footprintId, List<CreateBasketItemDto> itemsToAdd)
    {
        if (string.IsNullOrWhiteSpace(footprintId))
            throw new GlobalAppException("INVALID_FOOTPRINT");

        if (itemsToAdd == null || itemsToAdd.Count == 0)
            return;

        var normalized = NormalizeIncomingItems(itemsToAdd);

        var basket = await _basketRead.GetAsync(
            b => b.FootprintId == footprintId && !b.IsDeleted,
            q => q.Include(x => x.BasketItems),
            enableTracking: true
        );

        if (basket == null)
        {
            basket = new Basket
            {
                Id = Guid.NewGuid(),
                FootprintId = footprintId,
                CreatedDate = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow,
                IsDeleted = false,
                BasketItems = new List<BasketItem>()
            };

            await _basketWrite.AddAsync(basket);
            await _basketWrite.CommitAsync();
        }

        foreach (var kvp in normalized)
        {
            var (productId, variantId) = kvp.Key;
            var qty = kvp.Value;

            var existingActive = basket.BasketItems?
                .FirstOrDefault(bi => bi.ProductId == productId &&
                                      bi.ProductVariantId == variantId &&
                                      !bi.IsDeleted);

            var existingAny = basket.BasketItems?
                .FirstOrDefault(bi => bi.ProductId == productId &&
                                      bi.ProductVariantId == variantId);

            if (qty <= 0)
            {
                if (existingActive != null)
                {
                    existingActive.IsDeleted = true;
                    existingActive.DeletedDate = DateTime.UtcNow;
                    existingActive.LastUpdatedDate = DateTime.UtcNow;
                    await _basketItemWrite.UpdateAsync(existingActive);
                }
                continue;
            }

            if (existingAny == null)
            {
                var newItem = new BasketItem
                {
                    Id = Guid.NewGuid(),
                    BasketId = basket.Id,
                    ProductId = productId,
                    ProductVariantId = variantId,
                    Quantity = qty,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _basketItemWrite.AddAsync(newItem);
                basket.BasketItems!.Add(newItem);
            }
            else
            {
                if (existingAny.IsDeleted)
                {
                    existingAny.IsDeleted = false;
             
                }

                existingAny.Quantity = qty;
                existingAny.LastUpdatedDate = DateTime.UtcNow;
                await _basketItemWrite.UpdateAsync(existingAny);
            }
        }

        basket.LastUpdatedDate = DateTime.UtcNow;
        await _basketWrite.UpdateAsync(basket);
        await _basketWrite.CommitAsync();
    }

    // ======================================================
    //               REMOVE ITEM (User)
    // ======================================================
    public async Task RemoveBasketItemForUserAsync(string userId, string productId, string? productVariantId)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        if (!Guid.TryParse(productId, out var pid))
            throw new GlobalAppException("INVALID_PRODUCT_ID");

        var pvid = ParseNullableGuid(productVariantId);

        var basket = await _basketRead.GetAsync(
            b => b.UserId == uid && !b.IsDeleted,
            q => q.Include(x => x.BasketItems),
            enableTracking: true
        );

        if (basket?.BasketItems == null) return;

        var item = basket.BasketItems.FirstOrDefault(bi =>
            bi.ProductId == pid && bi.ProductVariantId == pvid && !bi.IsDeleted);

        if (item == null) return;

        item.IsDeleted = true;
        item.DeletedDate = DateTime.UtcNow;
        item.LastUpdatedDate = DateTime.UtcNow;

        await _basketItemWrite.UpdateAsync(item);
        await _basketWrite.CommitAsync();
    }

    // ======================================================
    //               REMOVE ITEM (Guest)
    // ======================================================
    public async Task RemoveBasketItemForGuestAsync(string footprintId, string productId, string? productVariantId)
    {
        if (string.IsNullOrWhiteSpace(footprintId))
            throw new GlobalAppException("INVALID_FOOTPRINT");

        if (!Guid.TryParse(productId, out var pid))
            throw new GlobalAppException("INVALID_PRODUCT_ID");

        var pvid = ParseNullableGuid(productVariantId);

        var basket = await _basketRead.GetAsync(
            b => b.FootprintId == footprintId && !b.IsDeleted,
            q => q.Include(x => x.BasketItems),
            enableTracking: true
        );

        if (basket?.BasketItems == null) return;

        var item = basket.BasketItems.FirstOrDefault(bi =>
            bi.ProductId == pid && bi.ProductVariantId == pvid && !bi.IsDeleted);

        if (item == null) return;

        item.IsDeleted = true;
        item.DeletedDate = DateTime.UtcNow;
        item.LastUpdatedDate = DateTime.UtcNow;

        await _basketItemWrite.UpdateAsync(item);
        await _basketWrite.CommitAsync();
    }

    // ======================================================
    //               REMOVE ALL (User)
    // ======================================================
    public async Task RemoveAllBasketItemsForUserAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        var basket = await _basketRead.GetAsync(
            b => b.UserId == uid && !b.IsDeleted,
            q => q.Include(x => x.BasketItems.Where(bi => !bi.IsDeleted)),
            enableTracking: true
        );

        if (basket?.BasketItems == null || basket.BasketItems.Count == 0)
            return;

        foreach (var bi in basket.BasketItems)
        {
            bi.IsDeleted = true;
            bi.DeletedDate = DateTime.UtcNow;
            bi.LastUpdatedDate = DateTime.UtcNow;
            await _basketItemWrite.UpdateAsync(bi);
        }

        basket.LastUpdatedDate = DateTime.UtcNow;
        await _basketWrite.UpdateAsync(basket);

        await _basketWrite.CommitAsync();
    }

    // ======================================================
    //               SET QUANTITY (User, delta)
    // ======================================================
    public async Task SetBasketItemQuantityAsync(string userId, string productId, string? productVariantId, int deltaQuantity)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        if (!Guid.TryParse(productId, out var pid))
            throw new GlobalAppException("INVALID_PRODUCT_ID");

        var pvid = ParseNullableGuid(productVariantId);

        var basket = await _basketRead.GetAsync(
            b => b.UserId == uid && !b.IsDeleted,
            q => q.Include(x => x.BasketItems.Where(bi => !bi.IsDeleted)),
            enableTracking: true
        ) ?? throw new GlobalAppException("BASKET_NOT_FOUND");

        var item = basket.BasketItems?.FirstOrDefault(bi =>
            bi.ProductId == pid && bi.ProductVariantId == pvid && !bi.IsDeleted)
            ?? throw new GlobalAppException("BASKET_ITEM_NOT_FOUND");

        var newQty = item.Quantity + deltaQuantity;

        if (newQty <= 0)
        {
            item.IsDeleted = true;
            item.DeletedDate = DateTime.UtcNow;
        }
        else
        {
            item.Quantity = newQty;
        }

        item.LastUpdatedDate = DateTime.UtcNow;
        await _basketItemWrite.UpdateAsync(item);
        await _basketWrite.CommitAsync();
    }

    // ======================================================
    //               GET (User)
    // ======================================================
    public async Task<BasketDto> GetBasketForUserAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        var basket = await _basketRead.GetAsync(
            b => b.UserId == uid && !b.IsDeleted,
            q => q
                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.Product)
                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.ProductVariant).ThenInclude(pv => pv.ProductColor).ThenInclude(b => b.Product)
                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.Product)
                .ThenInclude(p => p.Images)
                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.ProductVariant)
                .ThenInclude(pv => pv.OptionValues)
                .ThenInclude(ov => ov.OptionValue)

                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.ProductVariant)
                .ThenInclude(pv => pv.ProductColor)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)

                .AsSplitQuery(),
            enableTracking: false
        );

        return await MapBasketToDtoAsync(basket);
    }

    // ======================================================
    //               GET (Guest)
    // ======================================================
    public async Task<BasketDto> GetBasketForGuestAsync(string footprintId)
    {
        if (string.IsNullOrWhiteSpace(footprintId))
            throw new GlobalAppException("INVALID_FOOTPRINT");

        var basket = await _basketRead.GetAsync(
            b => b.FootprintId == footprintId && !b.IsDeleted,
            q => q
                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.Product)
                .ThenInclude(p => p.Images)

                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.ProductVariant)
                .ThenInclude(pv => pv.OptionValues)
                .ThenInclude(ov => ov.OptionValue)

                .Include(b => b.BasketItems.Where(bi => !bi.IsDeleted))
                .ThenInclude(bi => bi.ProductVariant)
                .ThenInclude(pv => pv.ProductColor)
                .ThenInclude(pc => pc.ColorImages)
                .ThenInclude(ci => ci.ProductImage)

                .AsSplitQuery(),
            enableTracking: false
        );

        return await MapBasketToDtoAsync(basket);
    }

    // ======================================================
    //               MERGE (Guest -> User)
    // ======================================================
    public async Task MergeBasketAsync(string userId, string footprintId)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        if (string.IsNullOrWhiteSpace(footprintId))
            return;

        var guestBasket = await _basketRead.GetAsync(
            b => b.FootprintId == footprintId && !b.IsDeleted,
            q => q.Include(x => x.BasketItems),
            enableTracking: true
        );

        if (guestBasket?.BasketItems == null || guestBasket.BasketItems.Count == 0)
            return;

        var userBasket = await _basketRead.GetAsync(
            b => b.UserId == uid && !b.IsDeleted,
            q => q.Include(x => x.BasketItems),
            enableTracking: true
        );

        if (userBasket == null)
        {
            userBasket = new Basket
            {
                Id = Guid.NewGuid(),
                UserId = uid,
                CreatedDate = DateTime.UtcNow,
                LastUpdatedDate = DateTime.UtcNow,
                IsDeleted = false,
                BasketItems = new List<BasketItem>()
            };

            await _basketWrite.AddAsync(userBasket);
            await _basketWrite.CommitAsync();
        }

        foreach (var guestItem in guestBasket.BasketItems.Where(x => !x.IsDeleted))
        {
            var pid = guestItem.ProductId;
            var pvid = guestItem.ProductVariantId;

            var existingAny = userBasket.BasketItems
                .FirstOrDefault(bi => bi.ProductId == pid && bi.ProductVariantId == pvid);

            if (existingAny == null)
            {
                var newItem = new BasketItem
                {
                    Id = Guid.NewGuid(),
                    BasketId = userBasket.Id,
                    ProductId = pid,
                    ProductVariantId = pvid,
                    Quantity = guestItem.Quantity,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _basketItemWrite.AddAsync(newItem);
                userBasket.BasketItems.Add(newItem);
            }
            else
            {
                if (existingAny.IsDeleted)
                {
                    existingAny.IsDeleted = false;
             
                    existingAny.Quantity = guestItem.Quantity;
                }
                else
                {
                    existingAny.Quantity += guestItem.Quantity;
                }

                existingAny.LastUpdatedDate = DateTime.UtcNow;
                await _basketItemWrite.UpdateAsync(existingAny);
            }
        }

        guestBasket.IsDeleted = true;
        guestBasket.DeletedDate = DateTime.UtcNow;
        guestBasket.LastUpdatedDate = DateTime.UtcNow;
        await _basketWrite.UpdateAsync(guestBasket);

        userBasket.LastUpdatedDate = DateTime.UtcNow;
        await _basketWrite.UpdateAsync(userBasket);

        await _basketWrite.CommitAsync();
    }

    // ======================================================
    //               HELPERS
    // ======================================================

    private static Dictionary<(Guid productId, Guid? variantId), int> NormalizeIncomingItems(IEnumerable<CreateBasketItemDto> items)
    {
        var dict = new Dictionary<(Guid, Guid?), int>();

        foreach (var it in items)
        {
            if (!Guid.TryParse(it.ProductId, out var pid))
                continue;

            var vid = ParseNullableGuid(it.ProductVariantId);

            // last wins (idempotent set)
            dict[(pid, vid)] = it.Quantity;
        }

        return dict;
    }

    private static Guid? ParseNullableGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;

        if (Guid.TryParse(value, out var gid)) return gid;
        throw new GlobalAppException("INVALID_VARIANT_ID");
    }

    private async Task<BasketDto> MapBasketToDtoAsync(Basket? basket)
    {
        if (basket == null)
        {
            return new BasketDto
            {
                UserId = "",
                FootprintId = "",
                BasketItems = new List<BasketItemDto>(),
                TotalAmount = 0m
            };
        }

        var result = new BasketDto
        {
            UserId = basket.UserId?.ToString() ?? "",
            FootprintId = basket.FootprintId ?? "",
            BasketItems = new List<BasketItemDto>(),
            TotalAmount = 0m
        };

        decimal total = 0m;

        foreach (var bi in basket.BasketItems ?? Enumerable.Empty<BasketItem>())
        {
            var product = bi.Product;
            if (product == null) continue;

            var pv = bi.ProductVariant;

            var modifier = pv?.PriceModifier ?? 0m;
            var unitPrice = product.Price + modifier;

            var dto = new BasketItemDto
            {
                Product = _mapper.Map<ProductDto>(product),
                // DTO-da bunun nullable olması daha doğrudur:
                ProductVariant = pv != null ? _mapper.Map<ProductVariantDto>(pv) : null,
                Price = unitPrice,
                CompareAtPrice = product.CompareAtPrice,
                TotalCompareAtPrice = (product.CompareAtPrice ?? 0m) * bi.Quantity,
                Quantity = bi.Quantity,
                FinalPrice = unitPrice * bi.Quantity
            };

            total += dto.FinalPrice;
            result.BasketItems.Add(dto);
        }

        result.TotalAmount = total;
        return result;
    }
}
