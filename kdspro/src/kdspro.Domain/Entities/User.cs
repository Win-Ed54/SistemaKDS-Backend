using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]

    public string Id {get; set;} = "";
    public string Username {get; set;} = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";

    public string PasswordHash {get; set;} = "";

    public string Role {get; set;} = "";

    public string ServiceScope { get; set; } = "hybrid";
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
    public bool IsDemoAccount { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public string CurrentSessionId { get; set; } = "";
}
