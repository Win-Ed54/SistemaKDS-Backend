using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using kdspro.Domain.Enums;
using MongoDB.Bson;

namespace kdspro.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(MongoDbContext context) : base(context, "Orders")
    {
        // Índice para optimizar consultas FIFO
        var indexKeys = Builders<Order>.IndexKeys
            .Ascending(o => o.Status)
            .Ascending(o => o.CreatedAt);

        var indexModel = new CreateIndexModel<Order>(
            indexKeys,
            new CreateIndexOptions { Name = "idx_orders_active_fifo" }
        );

        _collection.Indexes.CreateOne(indexModel);
    }

    /// <summary>
    /// Actualiza el estado y gestiona automáticamente los tiempos.
    /// </summary>
    public async Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq("_id", new ObjectId(id));

        var update = Builders<Order>.Update.Set(o => o.Status, status);

        var now = DateTime.UtcNow;

        //NUEVA LÓGICA DE TIEMPOS
        switch (status)
        {
            case OrderStatus.Preparing:
                // Inicio de cocina
                update = update.Set(o => o.StartedAt, now);
                break;

            case OrderStatus.Ready:
                // Pedido listo
                update = update.Set(o => o.ReadyAt, now);
                break;

            case OrderStatus.Delivered:
                // Pedido entregado
                update = update.Set(o => o.DeliveredAt, now);
                break;
        }

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Obtiene órdenes activas (para cocina).
    /// Solo Pending y Preparing.
    /// </summary>
    public async Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.In(o => o.Status, new[]
        {
            OrderStatus.Pending,
            OrderStatus.Preparing
        });

        return await _collection
            .Find(filter)
            .SortBy(o => o.CreatedAt) // FIFO
            .ToListAsync(ct);
    }

   
    /// <summary>
    /// Órdenes listas para recoger por el mesero.
    /// </summary>
    public async Task<List<Order>> GetReadyOrdersAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Status, OrderStatus.Ready);

        return await _collection
            .Find(filter)
            .SortBy(o => o.ReadyAt)
            .ToListAsync(ct);
    }

    //(historial)
    public async Task<List<Order>> GetHistoryAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.In(o => o.Status, new[]
        {
            OrderStatus.Delivered,
            OrderStatus.Cancelled
        });

        return await _collection
            .Find(filter)
            .SortByDescending(o => o.DeliveredAt)
            .ToListAsync(ct);
    }
}
