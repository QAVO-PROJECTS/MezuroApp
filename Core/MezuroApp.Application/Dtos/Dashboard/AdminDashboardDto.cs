namespace MezuroApp.Application.Dtos.Dashboard;


public sealed record AdminDashboardDto(
    AdminDashboardSummaryDto Summary,
    List<RevenueTrendItemDto> RevenueTrend,
    List<DailyOrdersItemDto> DailyOrders,
    List<MonthlyRefundItemDto> MonthlyRefunds,
    PaymentSuccessRateDto PaymentSuccessRate,
    List<TopCategoryItemDto> TopCategories,
    List<TopProductItemDto> TopProducts,
    AbandonedCartFunnelDto AbandonedCartFunnel,
    List<AverageOrderValueItemDto> AverageOrderValueTrend
);
public sealed record RevenueTrendItemDto(
    string Date,
    decimal Revenue
);

public sealed record DailyOrdersItemDto(
    string Date,
    int Count
);

public sealed record MonthlyRefundItemDto(
    string Date,
    int Count
);

public sealed record PaymentSuccessRateDto(
    int SuccessfulCount,
    int FailedCount,
    decimal SuccessRate
);

public sealed record TopCategoryItemDto(
    string CategoryName,
    int Quantity,
    decimal Percent
);

public sealed record TopProductItemDto(
    string ProductName,
    int Quantity
);

public sealed record AbandonedCartFunnelDto(
    int CartCreated,
    int CheckoutStarted,
    int CompletedOrders,
    decimal ConversionRate
);

public sealed record AverageOrderValueItemDto(
    string Date,
    decimal AverageValue
);