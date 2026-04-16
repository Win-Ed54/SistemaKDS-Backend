using MongoDB.Driver;
using kdspro.Domain.Entities;

namespace kdspro.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoDatabase database)
    {
        _database = database;
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");

    public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");

    public IMongoCollection<Table> Tables => _database.GetCollection<Table>("Tables");

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

    public IMongoCollection<KdsSettings> KdsSettings => _database.GetCollection<KdsSettings>("KdsSettings");
}
