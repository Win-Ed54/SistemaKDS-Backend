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
}