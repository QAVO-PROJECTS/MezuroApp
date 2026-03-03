using MezuroApp.Application.Dtos.Payment;

namespace MezuroApp.Application.Abstracts.Services;

public interface IUserCardService
{
    Task<List<UserCardDto>> GetMyCardsAsync(string userId, CancellationToken ct = default);
    Task SetDefaultAsync(string userId, Guid cardId, CancellationToken ct = default);
}