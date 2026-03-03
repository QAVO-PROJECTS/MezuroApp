using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    // Admin info
    public string? UserId { get; set; }
    public string? AdminName { get; set; }
    public string? SearchText { get; set; } // lower-case, orderNumber/couponCode/entityType/adminName vs

    // Module info
    public string Module { get; set; } = default!;      // Orders, Products...
    public string ActionType { get; set; } = default!;  // Update, StatusChange...

    // Entity info
    [BsonRepresentation(BsonType.String)]
    public Guid? EntityId { get; set; }

    // Changes
    public Dictionary<string, object>? OldValuesJson { get; set; }
    public Dictionary<string, object>? NewValuesJson { get; set; }

    // Request info
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}