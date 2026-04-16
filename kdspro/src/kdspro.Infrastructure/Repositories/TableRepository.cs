using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;

namespace kdspro.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de Mesas (Capa Infrastructure).
/// Hereda de GenericRepository para operaciones CRUD básicas y añade lógica de MongoDB.
/// </summary>
public class TableRepository : GenericRepository<Table>, ITableRepository
{
    /// <summary>
    /// Constructor que inyecta el contexto de base de datos y define la colección "Tables".
    /// </summary>
    public TableRepository(MongoDbContext context) : base(context, "Tables") { }

    /// <summary>
    /// Actualización parcial (PATCH) optimizada para cambiar el estado de una mesa.
    /// Utiliza Builders de MongoDB para una operación eficiente sin reescribir todo el documento.
    /// </summary>
    /// <param name="id">ID único del documento en MongoDB.</param>
    /// <param name="isActive">Nuevo estado de disponibilidad (true/false).</param>
    public async Task UpdateAvailabilityAsync(string id, bool isActive)
    {
        // 1. Definimos el filtro por ID (ObjectId)
        var filter = Builders<Table>.Filter.Eq(t => t.Id, id);
        
        // 2. Definimos la actualización atómica solo para el campo IsActive
        var update = Builders<Table>.Update.Set(t => t.IsActive, isActive);
        
        // 3. Ejecutamos la operación usando la _collection heredada de GenericRepository
        // Esto garantiza que el cambio sea inmediato en la base de datos de Docker
        await _collection.UpdateOneAsync(filter, update);
    }
    public async Task SetOccupiedAsync(int tableNumber, bool occupied)
    {
        var filter = Builders<Table>.Filter.Eq(t => t.Number, tableNumber);
        var update = Builders<Table>.Update
            .Set(t => t.IsOccupied, occupied);

        if (occupied)
        {
            update = update
                .Set(t => t.IsBeingCleaned, false)
                .Set(t => t.CleaningStartedAt, null)
                .Set(t => t.EstimatedCleaningMinutes, null);
        }

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task<Table?> GetByNumberAsync(int tableNumber)
    {
        var filter = Builders<Table>.Filter.Eq(t => t.Number, tableNumber);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task SeatGuestsAsync(
        int tableNumber,
        int partySize,
        int estimatedDiningMinutes,
        string notes,
        string assignedByName,
        string assignedWaiterId,
        string assignedWaiterName,
        DateTime occupiedSinceUtc)
    {
        var filter = Builders<Table>.Filter.Eq(t => t.Number, tableNumber);
        var update = Builders<Table>.Update
            .Set(t => t.IsOccupied, true)
            .Set(t => t.CurrentPartySize, partySize)
            .Set(t => t.OccupiedSince, occupiedSinceUtc)
            .Set(t => t.EstimatedDiningMinutes, estimatedDiningMinutes)
            .Set(t => t.HostNotes, notes ?? string.Empty)
            .Set(t => t.AssignedByName, assignedByName ?? string.Empty)
            .Set(t => t.AssignedWaiterId, assignedWaiterId ?? string.Empty)
            .Set(t => t.AssignedWaiterName, assignedWaiterName ?? string.Empty)
            .Set(t => t.IsBeingCleaned, false)
            .Set(t => t.CleaningStartedAt, null)
            .Set(t => t.EstimatedCleaningMinutes, null);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task StartCleaningAsync(
        int tableNumber,
        int estimatedCleaningMinutes,
        DateTime cleaningStartedAtUtc)
    {
        var filter = Builders<Table>.Filter.Eq(t => t.Number, tableNumber);
        var update = Builders<Table>.Update
            .Set(t => t.IsOccupied, true)
            .Set(t => t.IsBeingCleaned, true)
            .Set(t => t.CleaningStartedAt, cleaningStartedAtUtc)
            .Set(t => t.EstimatedCleaningMinutes, estimatedCleaningMinutes);

        await _collection.UpdateOneAsync(filter, update);
    }

    public async Task ClearServiceStateAsync(int tableNumber, bool occupied)
    {
        var filter = Builders<Table>.Filter.Eq(t => t.Number, tableNumber);
        var update = Builders<Table>.Update
            .Set(t => t.IsOccupied, occupied)
            .Set(t => t.CurrentPartySize, null)
            .Set(t => t.OccupiedSince, null)
            .Set(t => t.EstimatedDiningMinutes, null)
            .Set(t => t.HostNotes, string.Empty)
            .Set(t => t.AssignedByName, string.Empty)
            .Set(t => t.AssignedWaiterId, string.Empty)
            .Set(t => t.AssignedWaiterName, string.Empty)
            .Set(t => t.IsBeingCleaned, false)
            .Set(t => t.CleaningStartedAt, null)
            .Set(t => t.EstimatedCleaningMinutes, null);

        await _collection.UpdateOneAsync(filter, update);
    }
}
