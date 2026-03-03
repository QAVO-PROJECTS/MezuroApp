
using MezuroApp.Application.Dtos.Transaction;
using MezuroApp.Domain.HelperEntities;

namespace MezuroApp.Application.Abstracts.Services;

public interface IAdminTransactionService
{
    Task<PagedResult<AdminTransactionListItemDto>> GetTransactionsAsync(AdminTransactionListFilterDto f, CancellationToken ct);
    Task<AdminTransactionDetailDto> GetTransactionDetailAsync(string paymentTransactionId, CancellationToken ct);
    Task<AdminTransactionDashboardDto> GetDashboardAsync(AdminTransactionListFilterDto dto ,CancellationToken ct);
    

    // Create Refund (Epoint reverse)
    Task<AdminRefundResultDto> AdminReverseEpointAsync(AdminCreateRefundDto dto, CancellationToken ct);
}