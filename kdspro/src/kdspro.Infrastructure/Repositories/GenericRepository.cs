using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;

namespace kdspro.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    // protected permite que las clases hijas (como OrderRepository) la usen
    protected readonly IMongoCollection<T> _collection;

    public GenericRepository(MongoDbContext context, string collectionName)
    {
        // Obtenemos la colección dinámica de MongoDB
        _collection = context.Database.GetCollection<T>(collectionName);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _collection.Find(_ => true).ToListAsync(ct);
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        // MongoDB usa por defecto el campo "_id"
        var filter = Builders<T>.Filter.Eq("Id", id); 
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task CreateAsync(T entity, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(entity, null, ct);
    }

    public async Task UpdateAsync(string id, T entity, CancellationToken ct = default)
    {
        var filter = Builders<T>.Filter.Eq("Id", id);
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: ct);
    }
}