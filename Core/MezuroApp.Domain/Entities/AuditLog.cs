using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    // User info
    [BsonRepresentation(BsonType.String)]
    public string? UserId { get; set; }

    // Entity info
    public string EntityType { get; set; }   // Product, Order, User və s.
    
    [BsonRepresentation(BsonType.String)]
    public Guid? EntityId { get; set; }

    // Action
    public string Action { get; set; }        // CREATE, UPDATE, DELETE, LOGIN...

    // Changes
    public Dictionary<string, object> OldValues { get; set; }
    public Dictionary<string, object> NewValues { get; set; }

    // Request info
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}