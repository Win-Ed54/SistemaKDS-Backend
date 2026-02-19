namespace kdspro.Domain.Entities;

using kdspro.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
public class Order
{
    [BsonId]//Define esta propiedad como la llave  primaria
    [BsonRepresentation(BsonType.ObjectId)]//Permite que mongo genere el ID automaticamente
    public string? Id { get; set; } //Cambiado a nullable y sin string.Empty
    
    // Número de mesa para que el mesero sepa a dónde llevarlo
    public int TableNumber { get; set; }
    
    // Fecha y hora exacta (importante para el sistema FIFO: el primero que llega, primero sale)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Eliminamos la versión string y usamos solo el Enum
    // Este atributo guarda el nombre del estado en la BD para que sea legible
    // Una lista de los productos que pidieron
    public List<OrderItem> Items { get; set; } = new();

    public OrderStatus Status  { get; set; } = OrderStatus.Pending;
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Notes { get; set; } = string.Empty; // Ej: "Sin cebolla"
}

/*Aquí usamos una lista de OrderItem. Esto permite que una sola orden tenga 3 hamburguesas y 2 sodas, cada una con sus propias notas.*/