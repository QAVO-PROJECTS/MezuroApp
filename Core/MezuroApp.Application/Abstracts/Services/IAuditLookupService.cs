namespace MezuroApp.Application.Abstracts.Services
{
    public interface IAuditLookupService
    {
        Task<Dictionary<string, object>?> GetOldValuesAsync(string entityType, string actionType, string? id);
    }
}