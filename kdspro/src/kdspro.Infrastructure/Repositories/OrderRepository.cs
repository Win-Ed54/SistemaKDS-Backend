using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using kdspro.Domain.Enums;

namespace kdspro.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(MongoDbContext context) : base(context, "Orders")
    {
    }

    /// <summary>
    /// Actualiza el estado y gestiona automáticamente la auditoría de tiempo (FinishedAt)
    /// </summary>
    public async Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
        
        // 1. Preparamos la actualización del estado
        var update = Builders<Order>.Update.Set(o => o.Status, status);

        // 2. Lógica de Auditoría: Si está terminada o entregada, grabamos la fecha
        if (status == OrderStatus.Ready || status == OrderStatus.Delivered)
        {
            update = update.Set(o => o.FinishedAt, DateTime.UtcNow);
        }
        // Si vuelve a un estado previo, limpiamos la fecha de finalización
        else 
        {
            update = update.Set(o => o.FinishedAt, (DateTime?)null);
        }

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Obtiene órdenes activas (FIFO) ignorando las entregadas y las canceladas
    /// </summary>
    public async Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default)
    {
        // Filtramos: NO mostrar lo que ya se entregó ni lo que se canceló
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Delivered),
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Cancelled)
        );

        // Ordenamos por fecha de creación (FIFO: las más antiguas primero)
        var sort = Builders<Order>.Sort.Ascending(o => o.CreatedAt);

        // Importante: Retornamos List<Order> para coincidir con la Interfaz y evitar el error CS0738
        return await _collection.Find(filter)
                                 .Sort(sort)
                                 .ToListAsync(ct);
    }
}