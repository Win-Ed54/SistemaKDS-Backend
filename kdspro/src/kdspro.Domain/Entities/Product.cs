using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

/// <summary>
/// Entidad que representa un producto del menú (Módulo Menú - Mes 1).
/// Define la estructura base para hamburguesas, bebidas y acompañamientos.
/// </summary>
public class Product
{
    /// <summary>
    /// Identificador único del producto. 
    /// Se mapea como ObjectId en MongoDB para asegurar unicidad.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    /// <summary>
    /// Nombre comercial del producto (Ej: "Baconator").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Precio de venta al público.
    /// Se almacena como Decimal128 para evitar errores de redondeo en cálculos financieros.
    /// </summary>
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Price { get; set; }

    /// <summary>
    /// Descripción detallada de los ingredientes o contenido del producto.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Cantidad física disponible en inventario (Nuevo para Stock Automático).
    /// </summary>
    public int Stock { get; set; } = 0;

    /// <summary>
      /// El producto está disponible solo si el Stock es mayor a cero.
    /// Se marca como ignore para que MongoDB no intente guardarlo, ya que es calculado.
    /// </summary>
    [BsonIgnore]
    public bool IsAvailable => Stock > 0;
    
    /// <summary>
    /// Categoría para organizar el menú (Ej: "Hamburguesas", "Bebidas", "Postres").
    /// Facilita el filtrado en la terminal del mesero y reportes de ventas.
    /// </summary>
    public string Category { get; set; } = string.Empty;
}