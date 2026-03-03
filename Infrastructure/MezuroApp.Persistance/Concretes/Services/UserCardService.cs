using MezuroApp.Application.Abstracts.Repositories.UserCards;
using MezuroApp.Application.Abstracts.Services;
using MezuroApp.Application.Dtos.Payment;
using MezuroApp.Application.GlobalException;
using Microsoft.EntityFrameworkCore;

public sealed class UserCardService : IUserCardService
{
    private readonly IUserCardReadRepository _read;
    private readonly IUserCardWriteRepository _write;

    public UserCardService(IUserCardReadRepository read, IUserCardWriteRepository write)
    {
        _read = read;
        _write = write;
    }

    public async Task<List<UserCardDto>> GetMyCardsAsync(string userId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        var cards = await _read.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.UserId == uid)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedDate)
            .Select(x => new UserCardDto(
                x.Id,
                x.CardName,
                x.CardMask,
                x.CardExpiry,
                x.IsDefault
            ))
            .ToListAsync(ct);

        return cards;
    }

    public async Task SetDefaultAsync(string userId, Guid cardId, CancellationToken ct = default)
    {
        if (!Guid.TryParse(userId, out var uid))
            throw new GlobalAppException("INVALID_USER_ID");

        var cards = await _read.Query()
            .Where(x => !x.IsDeleted && x.UserId == uid)
            .ToListAsync(ct);

        if (cards.Count == 0)
            throw new GlobalAppException("CARD_NOT_FOUND");

        var target = cards.FirstOrDefault(x => x.Id == cardId);
        if (target == null)
            throw new GlobalAppException("CARD_NOT_FOUND");

        foreach (var c in cards)
            c.IsDefault = (c.Id == cardId);

        await _write.CommitAsync();
    }
}