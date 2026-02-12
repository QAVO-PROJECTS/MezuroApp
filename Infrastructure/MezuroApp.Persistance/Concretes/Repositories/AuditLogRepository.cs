using MezuroApp.Application.Abstracts.Repositories;
using MongoDB.Driver;

namespace MezuroApp.Persistance.Concretes.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditLog> _collection;

    public AuditLogRepository(MongoDbContext context)
    {
        _collection = context.AuditLogs;
    }

    public async Task AddAsync(AuditLog log)
    {
        await _collection.InsertOneAsync(log);
    }
}
