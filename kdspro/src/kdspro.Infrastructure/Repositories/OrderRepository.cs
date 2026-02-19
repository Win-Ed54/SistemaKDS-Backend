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
        // --- OPTIMIZACIÓN DE RENDIMIENTO (MES 2) ---
        // Creamos un índice compuesto: Filtra por Status y Ordena por CreatedAt.
        // Esto hace que la consulta FIFO sea instantánea sin importar el volumen de datos.
        var indexKeys = Builders<Order>.IndexKeys
            .Ascending(o => o.Status)
            .Ascending(o => o.CreatedAt);

        var indexModel = new CreateIndexModel<Order>(indexKeys, new CreateIndexOptions { Name = "idx_orders_active_fifo" });
        _collection.Indexes.CreateOne(indexModel);
    }

    /// <summary>
    /// Actualiza el estado y gestiona automáticamente la auditoría de tiempo (FinishedAt)
    /// </summary>
    public async Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
    
        // 1. Determinamos la fecha de finalización (solo si pasa a Ready o Delivered)
        DateTime? finishedDate = (status == OrderStatus.Ready || status == OrderStatus.Delivered) 
                                ? DateTime.UtcNow 
                                : null;

        // 2. Única instrucción de actualización (Atómica)
        var update = Builders<Order>.Update
           .Set(o => o.Status, status)
           .Set(o => o.FinishedAt, finishedDate);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Obtiene órdenes activas (FIFO) ignorando las entregadas y las canceladas
    /// </summary>
    public async Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Delivered),
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Cancelled)
        );

        var sort = Builders<Order>.Sort.Ascending(o => o.CreatedAt);

        return await _collection.Find(filter)
                                 .Sort(sort)
                                 .ToListAsync(ct);
    }
}