namespace MezuroApp.Application.Abstracts.Services;

public interface IAuditLogService
{
    Task LogAsync(AuditLog log);
}