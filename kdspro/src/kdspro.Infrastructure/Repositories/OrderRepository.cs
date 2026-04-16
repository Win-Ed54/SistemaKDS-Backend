using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using kdspro.Domain.Enums;
using MongoDB.Bson;

namespace kdspro.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    private readonly IMongoCollection<BsonDocument> _counters;

    public OrderRepository(MongoDbContext context) : base(context, "Orders")
    {
        _counters = context.Database.GetCollection<BsonDocument>("Counters");

        var indexKeys = Builders<Order>.IndexKeys
            .Ascending(o => o.Status)
            .Ascending(o => o.CreatedAt);

        var indexModel = new CreateIndexModel<Order>(
            indexKeys,
            new CreateIndexOptions { Name = "idx_orders_active_fifo" }
        );

        _collection.Indexes.CreateOne(indexModel);
    }

    // --- IMPLEMENTACIÓN DE LA INTERFAZ ---

    // 1. Método requerido: UpdateStatusAsync (3 parámetros)
    public async Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct)
    {
        await ExecuteUpdateStatusAsync(id, status, null, null, ct);
    }

    // 2. Método requerido: UpdateStatusAsync (5 parámetros)
    // Nota: Eliminamos los "= null" para que coincida exactamente con la interfaz
    public async Task UpdateStatusAsync(string id, OrderStatus status, DateTime? startedAt, DateTime? readyAt, CancellationToken ct)
    {
        await ExecuteUpdateStatusAsync(id, status, startedAt, readyAt, ct);
    }

    // 3. Método requerido: UpdateStatusWithTimeAsync
    public async Task UpdateStatusWithTimeAsync(string id, OrderStatus status, DateTime? startedAt, DateTime? readyAt, CancellationToken ct)
    {
        await ExecuteUpdateStatusAsync(id, status, startedAt, readyAt, ct);
    }

    // --- LÓGICA PRIVADA (Para no repetir código) ---
    private async Task ExecuteUpdateStatusAsync(string id, OrderStatus status, DateTime? startedAt, DateTime? readyAt, CancellationToken ct)
    {
        var filter = Builders<Order>.Filter.Eq("_id", new ObjectId(id));
        var update = Builders<Order>.Update.Set(o => o.Status, status);
        var now = DateTime.UtcNow;

        switch (status)
        {
            case OrderStatus.Preparing:
                update = update.Set(o => o.StartedAt, startedAt ?? now);
                break;

            case OrderStatus.Ready:
                update = update.Set(o => o.ReadyAt, readyAt ?? now);
                break;

            case OrderStatus.Delivered:
                update = update.Set(o => o.DeliveredAt, now);
                break;
        }

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    // --- OTROS MÉTODOS ---

    public async Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.In(o => o.Status, new[]
        {
            OrderStatus.Pending,
            OrderStatus.Preparing,
            OrderStatus.Ready
        });

        return await _collection.Find(filter).SortBy(o => o.CreatedAt).ToListAsync(ct);
    }

    public async Task<List<Order>> GetReadyOrdersAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Status, OrderStatus.Ready);
        return await _collection.Find(filter).SortBy(o => o.ReadyAt).ToListAsync(ct);
    }

    public async Task<List<Order>> GetHistoryAsync(CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.In(o => o.Status, new[]
        {
            OrderStatus.Delivered,
            OrderStatus.Cancelled
        });

        return await _collection.Find(filter).SortByDescending(o => o.DeliveredAt).ToListAsync(ct);
    }

    public async Task<bool> HasActiveOrdersForTableAsync(int tableNumber, string excludeOrderId)
    {
        var filters = new List<FilterDefinition<Order>>
        {
            Builders<Order>.Filter.Eq(o => o.TableNumber, tableNumber),
            Builders<Order>.Filter.Lt(o => o.Status, OrderStatus.Delivered)
        };

        if (!string.IsNullOrWhiteSpace(excludeOrderId))
        {
            filters.Add(Builders<Order>.Filter.Ne(o => o.Id, excludeOrderId));
        }

        var filter = Builders<Order>.Filter.And(filters);
        return await _collection.Find(filter).AnyAsync();


    }

    public async Task<bool> HasNewerOrdersForTableAsync(int tableNumber, DateTime createdAfter, string excludeOrderId)
    {
        var filters = new List<FilterDefinition<Order>>
        {
            Builders<Order>.Filter.Eq(o => o.TableNumber, tableNumber),
            Builders<Order>.Filter.Gt(o => o.CreatedAt, createdAfter)
        };

        if (!string.IsNullOrWhiteSpace(excludeOrderId))
        {
            filters.Add(Builders<Order>.Filter.Ne(o => o.Id, excludeOrderId));
        }

        var filter = Builders<Order>.Filter.And(filters);

        return await _collection.Find(filter).AnyAsync();
    }

    public async Task<List<Order>> GetOrdersByWaiterAsync(string waiterId)
    {
        return await _collection
            .Find(o => o.WaiterId == waiterId || o.WaiterName == waiterId)
            .ToListAsync();
    }

    public async Task<int> GetNextCorrelativeNumberAsync(CancellationToken ct = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", "orders");
        var update = Builders<BsonDocument>.Update.Inc("seq", 1);
        var options = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _counters.FindOneAndUpdateAsync(filter, update, options, ct);
        return result["seq"].AsInt32;
    }

    public async Task MarkAsPaidAsync(string id, string paidByName, string paymentMethod, string receiptNumber, string documentType, bool invoiceRequested, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq("_id", new ObjectId(id));
        var update = Builders<Order>.Update
            .Set(o => o.IsPaid, true)
            .Set(o => o.PaidAt, DateTime.UtcNow)
            .Set(o => o.PaidByName, paidByName)
            .Set(o => o.PaymentMethod, paymentMethod)
            .Set(o => o.ReceiptNumber, receiptNumber)
            .Set(o => o.DocumentType, documentType)
            .Set(o => o.InvoiceRequested, invoiceRequested);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task SetPreparedByAsync(string id, string preparedByName, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq("_id", new ObjectId(id));
        var update = Builders<Order>.Update.Set(o => o.PreparedByName, preparedByName);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task SetCancelledByAsync(string id, string cancelledByName, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq("_id", new ObjectId(id));
        var update = Builders<Order>.Update.Set(o => o.CancelledByName, cancelledByName);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task MarkCleanupCompletedForTableAsync(int tableNumber, CancellationToken ct = default)
    {
        var cleanupPendingFilter = Builders<Order>.Filter.Or(
            Builders<Order>.Filter.Eq(o => o.IsCleanupCompleted, false),
            Builders<Order>.Filter.Exists(nameof(Order.IsCleanupCompleted), false)
        );

        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.TableNumber, tableNumber),
            Builders<Order>.Filter.Eq(o => o.Status, OrderStatus.Delivered),
            Builders<Order>.Filter.Eq(o => o.IsPaid, true),
            cleanupPendingFilter
        );

        var update = Builders<Order>.Update
            .Set(o => o.IsCleanupCompleted, true)
            .Set(o => o.CleanupCompletedAt, DateTime.UtcNow);

        await _collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }

    public async Task<bool> HasPendingPaymentForTableAsync(int tableNumber, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.TableNumber, tableNumber),
            Builders<Order>.Filter.Eq(o => o.Status, OrderStatus.Delivered),
            Builders<Order>.Filter.Eq(o => o.IsPaid, false)
        );

        return await _collection.Find(filter).AnyAsync(ct);
    }

}
