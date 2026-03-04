namespace MezuroApp.Application.Abstracts.Services;

public interface IAuditHelper
{
    Task LogAsync(
        string entityType,
        string action,
        string eventName,
        Guid? entityId = null,
        Dictionary<string,object>? oldValues = null,
        Dictionary<string,object>? newValues = null
    );
}