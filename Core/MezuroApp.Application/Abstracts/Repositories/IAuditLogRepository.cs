namespace MezuroApp.Application.Abstracts.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
}