using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

public class RefreshToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string UserId    { get; set; } = "";
    public string Token     { get; set; } = "";
    public DateTime Expires { get; set; }
    public bool IsRevoked   { get; set; } = false;

    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsActive  => !IsRevoked && !IsExpired;
}
