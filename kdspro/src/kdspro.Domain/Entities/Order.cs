using kdspro.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

[BsonIgnoreExtraElements]
public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int TableNumber { get; set; }

    // --- TIEMPOS DE AUDITORÍA Y KPIs ---

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// NUEVO: Momento en que la cocina inicia la preparación
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Registra el momento en que la cocina marcó la orden como "Ready"
    /// </summary>
    public DateTime? ReadyAt { get; set; }

    /// <summary>
    /// Registra el momento en que el mesero entregó el pedido
    /// </summary>
    [BsonElement("FinishedAt")]
    public DateTime? DeliveredAt { get; set; }

    // --- IDENTIFICACIÓN ---

    public string CustomerName { get; set; } = "Cliente";
    public string WaiterName { get; set; } = string.Empty;

    // --- CONTENIDO ---

    public List<OrderItem> Items { get; set; } = new();

    [BsonRepresentation(BsonType.String)]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // --- LÓGICA DE NEGOCIO ---

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalAmount => Items.Sum(i => i.UnitPrice * i.Quantity);

    /// <summary>
    /// MEJORADO: ahora usa StartedAt si existe
    /// </summary>
    public bool IsOverdue
    {
        get
        {
            var start = StartedAt ?? CreatedAt;
            return (DateTime.UtcNow - start).TotalMinutes > 15;
        }
    }
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    public List<string> Modifiers { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public bool IsPrepared { get; set; } = false;
}
