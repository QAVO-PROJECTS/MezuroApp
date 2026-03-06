namespace MezuroApp.Application.Dtos.Dashboard;

public sealed record AdminDashboardSummaryDto(
    decimal Revenue,
    int Orders,
    decimal SuccessRate,
    decimal RefundRate,
    decimal RevenueChangePercent,
    decimal OrdersChangePercent,
    decimal SuccessRateChangePercent,
    decimal RefundRateChangePercent
);