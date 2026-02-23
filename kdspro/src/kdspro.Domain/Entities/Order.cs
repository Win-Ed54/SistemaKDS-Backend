namespace kdspro.Domain.Entities;

using kdspro.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

/// <summary>
/// Entidad principal que representa un pedido en el sistema KDS (Módulo Pedidos - Mes 1 y 2).
/// Contiene la lógica para el seguimiento de tiempos, estados y cálculos financieros.
/// </summary>
public class Order
{
    /// <summary>
    /// Identificador único de la orden generado por MongoDB (ObjectId).
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    /// <summary>
    /// Número de la mesa física asignada al pedido.
    /// </summary>
    public int TableNumber { get; set; }
    
    // --- TIEMPOS Y FIFO ---
    
    /// <summary>
    /// Fecha y hora exacta de creación del pedido. 
    /// Es la base para el ordenamiento FIFO (First-In, First-Out) en la pantalla de cocina.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha y hora en que la orden fue marcada como "Lista". 
    /// Permite medir el tiempo de preparación (KPIs de eficiencia).
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    // --- IDENTIFICACIÓN ---
    
    /// <summary>
    /// Nombre del cliente para personalizar el servicio o llamar el pedido.
    /// </summary>
    public string CustomerName { get; set; } = "Cliente";

    /// <summary>
    /// Nombre del mesero que registró la orden (Crucial para notificaciones SignalR).
    /// </summary>
    public string WaiterName { get; set; } = string.Empty;

    // --- CONTENIDO ---
    
    /// <summary>
    /// Lista de productos incluidos en el pedido.
    /// </summary>
    public List<OrderItem> Items { get; set; } = new();
    
    /// <summary>
    /// Estado actual del flujo de trabajo (Pendiente, Cocinando, Listo, etc.).
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // --- LÓGICA DE NEGOCIO ---
    
    /// <summary>
    /// Cálculo automático del monto total de la orden basado en sus ítems.
    /// Se almacena como Decimal128 para garantizar precisión financiera en MongoDB.
    /// </summary>
    [BsonRepresentation(BsonType.Decimal128)] 
    public decimal TotalAmount => Items.Sum(i => i.UnitPrice * i.Quantity);

    /// <summary>
    /// Propiedad calculada que determina si la orden ha excedido el tiempo estándar (15 min).
    /// Se utiliza en el Frontend para resaltar el ticket en color ROJO (Requisito Técnico 2).
    /// </summary>
    public bool IsOverdue => (DateTime.UtcNow - CreatedAt).TotalMinutes > 15;
}

/// <summary>
/// Representa un producto específico dentro de una orden, incluyendo su personalización.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// ID de referencia al producto original del catálogo.
    /// </summary>
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del producto capturado al momento del pedido (Desacoplado del catálogo).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;
    
    /// <summary>
    /// Cantidad solicitada de este producto.
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// Precio unitario histórico al momento de la venta.
    /// </summary>
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    // --- PERSONALIZACIÓN ---
    
    /// <summary>
    /// Lista de modificaciones rápidas (Ej: ["Sin Cebolla", "Extra Queso"]).
    /// </summary>
    public List<string> Modifiers { get; set; } = new();

    /// <summary>
    /// Comentarios adicionales o instrucciones especiales del cliente.
    /// </summary>
    public string Notes { get; set; } = string.Empty; 

    /// <summary>
    /// Indica si este ítem específico ya ha sido preparado (Control de platos individuales).
    /// </summary>
    public bool IsPrepared { get; set; } = false;
}