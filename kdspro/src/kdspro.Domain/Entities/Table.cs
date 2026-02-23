using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace kdspro.Domain.Entities;

/// <summary>
/// Entidad que representa una mesa física en el restaurante (Requisito Mes 1).
/// Permite gestionar la capacidad y disponibilidad del local.
/// </summary>
public class Table
{
    /// <summary>
    /// Identificador único generado automáticamente por MongoDB (ObjectId).
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Número identificador de la mesa (Ej: 1, 2, 3). 
    /// Es el dato que el mesero verá rápidamente en la app.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Nombre descriptivo de la ubicación (Ej: "Mesa VIP", "Terraza 1").
    /// Ayuda al personal a localizar el pedido físicamente.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Estado de disponibilidad de la mesa (Requisito: Activar/Desactivar).
    /// Si es 'false', la mesa no aparecerá en la terminal del mesero para nuevas órdenes.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Cantidad máxima de personas que pueden sentarse en esta mesa.
    /// Dato clave para la gestión de reservas en módulos futuros.
    /// </summary>
    public int Capacity { get; set; } = 4;
}