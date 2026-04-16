using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

[BsonIgnoreExtraElements]
public class KdsSettings
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "default";

    public string ServiceMode { get; set; } = "quick-service";

    public int MaxDistinctItems { get; set; }

    public int MaxTotalUnits { get; set; }

    public int MaxQuantityPerProduct { get; set; }

    public int LargeOrderUnitsWarning { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
