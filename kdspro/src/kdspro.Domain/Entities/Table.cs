using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

public class Table
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Capacity { get; set; } = 4;

    // ✅ NUEVO: indica si la mesa tiene una orden activa en este momento
    public bool IsOccupied { get; set; } = false;
}