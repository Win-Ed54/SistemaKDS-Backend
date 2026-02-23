using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;

namespace kdspro.Infrastructure.Repositories;

/// <summary>
/// Implementación base para el patrón Repositorio Genérico (Capa Infrastructure).
/// Proporciona operaciones CRUD estándar sobre cualquier colección de MongoDB.
/// </summary>
/// <typeparam name="T">Entidad de dominio que debe ser una clase (Product, Order, Table).</typeparam>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    /// <summary>
    /// Acceso a la colección de MongoDB. 
    /// 'protected' permite que repositorios especializados (ej: OrderRepository) añadan lógica propia.
    /// </summary>
    protected readonly IMongoCollection<T> _collection;

    /// <summary>
    /// Inicializa la colección dinámica basada en el nombre de la "tabla" proporcionado.
    /// </summary>
    /// <param name="context">Contexto de base de datos inyectado.</param>
    /// <param name="collectionName">Nombre físico de la colección en MongoDB (ej: "Orders").</param>
    public GenericRepository(MongoDbContext context, string collectionName)
    {
        // Conexión dinámica: Obtenemos el acceso a la colección específica para el tipo T.
        _collection = context.Database.GetCollection<T>(collectionName);
    }

    /// <summary>
    /// Recupera todos los documentos de la colección de forma asincrónica.
    /// </summary>
    /// <param name="ct">Token para cancelar la operación si el cliente (React) cierra la petición.</param>
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
    {
        // Buscamos todos los documentos sin filtros (esto equivale a un SELECT *).
        return await _collection.Find(_ => true).ToListAsync(ct);
    }

    /// <summary>
    /// Busca un documento por su identificador único.
    /// </summary>
    /// <param name="id">ID del documento (ObjectId mapeado como string).</param>
    public async Task<T?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        // MongoDB identifica los documentos por el campo "Id".
        var filter = Builders<T>.Filter.Eq("Id", id); 
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Inserta un nuevo objeto en la base de datos de forma atómica.
    /// </summary>
    public async Task CreateAsync(T entity, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(entity, null, ct);
    }

    /// <summary>
    /// Reemplaza un documento completo existente por una nueva versión.
    /// </summary>
    /// <param name="id">ID del documento a reemplazar.</param>
    /// Objeto con los nuevos datos.</param>
    public async Task UpdateAsync(string id, T entity, CancellationToken ct = default)
    {
        var filter = Builders<T>.Filter.Eq("Id", id);
        // ReplaceOne sobrescribe el documento completo para asegurar consistencia.
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: ct);
    }
}
