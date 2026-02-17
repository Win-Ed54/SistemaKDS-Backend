using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using kdspro.Domain.Enums;

namespace kdspro.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    // Pasamos el contexto y el nombre "Orders" a la clase base
    public OrderRepository(MongoDbContext context) : base(context, "Orders")
    {
    }

    // Método específico de negocio que no es un CRUD genérico
    public async Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
        var update = Builders<Order>.Update.Set("Status", status);
        
        // Usamos _collection que heredamos de GenericRepository
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}