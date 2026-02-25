using kdspro.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

/// <summary>
/// Entidad principal que representa un pedido en el sistema KDS (Módulo Pedidos - Mes 1, 2 y 3).
/// Orquesta el ciclo de vida desde la toma del pedido hasta la entrega final en mesa.
/// </summary>
public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public int TableNumber { get; set; }
    
    // --- TIEMPOS DE AUDITORÍA Y KPIs ---
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Registra el momento en que la cocina marcó la orden como "Ready" (Lista para servir).
    /// </summary>
    public DateTime? ReadyAt { get; set; }

    /// <summary>
    /// Registra el momento exacto en que el mesero entregó el pedido (Estado Finished).
    /// </summary>
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
    /// Requisito Técnico 2: Resalta el ticket en ROJO si excede los 15 minutos de preparación.
    /// </summary>
    public bool IsOverdue => (DateTime.UtcNow - CreatedAt).TotalMinutes > 15;
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
