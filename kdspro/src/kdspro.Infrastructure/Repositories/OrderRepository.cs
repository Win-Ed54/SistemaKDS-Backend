using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using kdspro.Domain.Enums;

namespace kdspro.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _orders;

    public OrderRepository(MongoDbContext context)
    {
        _orders = context.Orders;
    }

    public async Task<List<Order>> GetAllAsync() => 
        await _orders.Find(_ => true).ToListAsync();

    public async Task CreateAsync(Order order) => 
        await _orders.InsertOneAsync(order);

    public async Task UpdateStatusAsync(string id, OrderStatus status)
    {
        var filter = Builders<Order>.Filter.Eq("_id", id); // O el nombre de tu campo ID
        var update = Builders<Order>.Update.Set("Status", status);
        await _orders.UpdateOneAsync(filter, update);
    }
}