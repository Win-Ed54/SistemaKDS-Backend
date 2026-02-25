using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using kdspro.Domain.Enums;

namespace kdspro.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de Órdenes.
/// Gestiona la persistencia en MongoDB sincronizando los estados con ReadyAt y DeliveredAt.
/// </summary>
public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(MongoDbContext context) : base(context, "Orders")
    {
        // Índice para optimizar consultas FIFO
        var indexKeys = Builders<Order>.IndexKeys
            .Ascending(o => o.Status)
            .Ascending(o => o.CreatedAt);

        var indexModel = new CreateIndexModel<Order>(indexKeys, new CreateIndexOptions { Name = "idx_orders_active_fifo" });
        _collection.Indexes.CreateOne(indexModel);
    }

    /// <summary>
    /// Actualiza el estado y gestiona automáticamente los tiempos de cocina y entrega.
    /// </summary>
    public async Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
        
        // Iniciamos la definición de actualización con el cambio de estado
        var update = Builders<Order>.Update.Set(o => o.Status, status);

        // LÓGICA DE TIEMPOS SEGÚN TU ENTIDAD 'ORDER'
        if (status == OrderStatus.Ready)
        {
            // El cocinero despacha -> Registramos ReadyAt
            update = update.Set(o => o.ReadyAt, DateTime.UtcNow);
        }
        else if (status == OrderStatus.Delivered)
        {
            // El mesero entrega en mesa -> Registramos DeliveredAt
            update = update.Set(o => o.DeliveredAt, DateTime.UtcNow);
        }

        // Ejecución atómica en MongoDB
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Recupera órdenes activas para el KDS (Excluye Delivered y Cancelled).
    /// </summary>
    public async Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Delivered),
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Cancelled)
        );

        return await _collection.Find(filter)
                                 .SortBy(o => o.CreatedAt)
                                 .ToListAsync(ct);
    }
}
