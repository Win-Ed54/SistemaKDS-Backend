using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

[BsonIgnoreExtraElements]
public class Ingredient
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = "unidad";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Stock { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;
}
