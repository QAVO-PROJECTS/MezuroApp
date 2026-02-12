using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var settings = configuration.GetSection("MongoSettings");
        var client = new MongoClient(settings["ConnectionString"]);
        
        _database = client.GetDatabase(settings["DatabaseName"]);
    }

    public IMongoCollection<AuditLog> AuditLogs =>
        _database.GetCollection<AuditLog>("audit_logs");
}