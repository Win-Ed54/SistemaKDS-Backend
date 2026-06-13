using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

public class OrderIngredientReservation
{
    public string IngredientId { get; set; } = string.Empty;

    public string IngredientName { get; set; } = string.Empty;

    public string Unit { get; set; } = "unidad";

    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal Quantity { get; set; }
}
