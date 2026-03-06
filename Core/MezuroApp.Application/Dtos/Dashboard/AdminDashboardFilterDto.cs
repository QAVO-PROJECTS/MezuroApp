namespace MezuroApp.Application.Dtos.Dashboard;

public sealed class AdminDashboardFilterDto
{
    public string? From { get; set; } // dd.MM.yyyy
    public string? To { get; set; }   // dd.MM.yyyy
}