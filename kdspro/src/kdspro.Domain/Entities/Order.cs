namespace kdspro.Domain.Entities;

using kdspro.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    public int TableNumber { get; set; }
    
    // --- TIEMPOS Y FIFO ---
    // Fecha de entrada a cocina (Ordenamiento FIFO estricto)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Fecha en que el cocinero marcó "Ready". Permite calcular KPIs de velocidad.
    public DateTime? FinishedAt { get; set; }

    // --- IDENTIFICACIÓN ---
    // Nombre del cliente para llamar al pedido (ej: "Orden de Juan")
    public string CustomerName { get; set; } = "Cliente";

    // Nombre/ID del mesero que tomó la orden (para notificaciones de SignalR)
    public string WaiterName { get; set; } = string.Empty;

    // --- CONTENIDO ---
    public List<OrderItem> Items { get; set; } = new();
    
    [BsonRepresentation(BsonType.String)]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // --- LÓGICA DE NEGOCIO ---
    // Total calculado automáticamente sumando los subtotales de los items
    public decimal TotalAmount => Items.Sum(i => i.UnitPrice * i.Quantity);

    // Propiedad calculada: ¿Lleva más de 15 minutos esperando? (Para poner el ticket en ROJO)
    public bool IsOverdue => (DateTime.UtcNow - CreatedAt).TotalMinutes > 15;
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    
    // Guardamos el nombre aquí por si el producto original se borra o cambia de nombre
    public string ProductName { get; set; } = string.Empty;
    
    public int Quantity { get; set; }
    
    // Precio al momento de la compra (Auditoría financiera)
    public decimal UnitPrice { get; set; }

    // --- PERSONALIZACIÓN ---
    // Lista de ingredientes extra o eliminados (ej: ["Sin Tomate", "Extra Tocino"])
    public List<string> Modifiers { get; set; } = new();

    // Notas manuales largas (ej: "Término medio, alérgico a la mostaza")
    public string Notes { get; set; } = string.Empty; 

    // Estado individual del plato (Opcional: permite que la soda salga antes que la pizza)
    public bool IsPrepared { get; set; } = false;
}