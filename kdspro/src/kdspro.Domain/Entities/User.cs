using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]

    public string Id {get; set;} = "";
    public string Username {get; set;} = "";

    public string PasswordHash {get; set;} = "";

    public string Role {get; set;} = "";

    public string ServiceScope { get; set; } = "hybrid";

    public string CurrentSessionId { get; set; } = "";
}
