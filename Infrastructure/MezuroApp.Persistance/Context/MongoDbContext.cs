using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IConfiguration _configuration;

    public MongoDbContext(IConfiguration configuration)
    {
        _configuration = configuration;

        var settings = configuration.GetSection("MongoSettings");
        var client = new MongoClient(settings["ConnectionString"]);
        _database = client.GetDatabase(settings["DatabaseName"]);
    }

    public IMongoCollection<AuditLog> AuditLogs =>
        _database.GetCollection<AuditLog>(
            _configuration["MongoSettings:AuditLogCollection"] ?? "audit_logs"
        );
}