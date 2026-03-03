namespace MezuroApp.Application.Dtos.Transaction;

public sealed record AdminTransactionDashboardDto(
    int TotalTransactions,
    decimal TotalRevenue,
    int PendingPayments,
    decimal RefundedAmount,

    decimal TotalTransactionsChangePercent,
    decimal RevenueChangePercent,
    decimal PendingChangePercent,
    decimal RefundedChangePercent
);