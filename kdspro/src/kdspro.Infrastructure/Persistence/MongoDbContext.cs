using MongoDB.Driver;
using kdspro.Domain.Entities;
using Microsoft.Extensions.Configuration; // Esto quitará el error de IConfiguration

namespace kdspro.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public IMongoDatabase Database => _database;

    public MongoDbContext(IConfiguration configuration)
    {
        // Leemos la configuración del appsettings.json
        var connectionString = configuration.GetSection("MongoDB:ConnectionString").Value;
        var databaseName = configuration.GetSection("MongoDB:DatabaseName").Value;

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    // Definimos las colecciones (las "tablas" de MongoDB)
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");
    public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");
}