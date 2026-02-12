namespace MezuroApp.Application.Abstracts.Services
{
    public interface IAuditLookupService
    {
      
        Task<Dictionary<string, object>?> GetOldValuesAsync(string controller, string action, string? id);
    }
}