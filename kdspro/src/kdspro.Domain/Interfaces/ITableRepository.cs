using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

/// <summary>
/// Interfaz específica para el repositorio de Mesas (Requisito Mes 1).
/// Hereda las operaciones CRUD básicas de IGenericRepository e incluye lógica personalizada.
/// </summary>
public interface ITableRepository : IGenericRepository<Table>
{
    /// <summary>
    /// Actualización parcial (PATCH) para cambiar el estado de servicio de una mesa.
    /// Crucial para el Módulo Administrativo: permite habilitar o inhabilitar mesas 
    /// para que no reciban nuevas órdenes si están en limpieza o reservadas.
    /// </summary>
    /// <param name="id">Identificador único de la mesa en MongoDB (ObjectId).</param>
    /// <param name="isActive">Nuevo estado de disponibilidad (true = disponible, false = fuera de servicio).</param>
    /// <returns>Tarea asincrónica que representa la operación en la base de datos.</returns>
    Task UpdateAvailabilityAsync(string id, bool isActive);

    Task SetOccupiedAsync(int tableNumber, bool occupied);
    Task<Table?> GetByNumberAsync(int tableNumber);
    Task SeatGuestsAsync(
        int tableNumber,
        int partySize,
        int estimatedDiningMinutes,
        string notes,
        string assignedByName,
        string assignedWaiterId,
        string assignedWaiterName,
        DateTime occupiedSinceUtc);
    Task StartCleaningAsync(
        int tableNumber,
        int estimatedCleaningMinutes,
        DateTime cleaningStartedAtUtc);
    Task ClearServiceStateAsync(int tableNumber, bool occupied);
}
