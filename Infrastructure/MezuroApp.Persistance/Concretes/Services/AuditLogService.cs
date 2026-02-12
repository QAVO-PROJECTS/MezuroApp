using MezuroApp.Application.Abstracts.Repositories;
using MezuroApp.Application.Abstracts.Services;

namespace MezuroApp.Persistance.Concretes.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(AuditLog log)
    {
        await _repository.AddAsync(log);
    }
}
