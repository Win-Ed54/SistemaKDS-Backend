using MongoDB.Driver;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;

namespace kdspro.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de Productos (Capa Infrastructure).
/// Gestiona la persistencia del catálogo del menú en MongoDB.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly MongoDbContext _context;

    /// <summary>
    /// Constructor que inyecta el contexto de base de datos de MongoDB.
    /// </summary>
    public ProductRepository(MongoDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Recupera todos los productos (Hamburguesas, Bebidas, etc.) para el menú del mesero.
    /// </summary>
    public async Task<List<Product>> GetAllAsync()
    {
        // Filtro vacío (_ => true) para obtener todos los documentos de la colección
        return await _context.Products.Find(_ => true).ToListAsync();
    }

    /// <summary>
    /// Registra un nuevo producto de forma atómica en la base de datos.
    /// </summary>
    public async Task CreateAsync(Product product)
    {
        await _context.Products.InsertOneAsync(product);
    }

    /// <summary>
    /// REQUISITO MES 1: Gestión de Stock Crítico.
    /// Actualiza únicamente el campo de disponibilidad (isAvailable) mediante una operación de set.
    /// </summary>
    /// <param name="id">ID del producto en MongoDB.</param>
    /// <param name="isAvailable">Nuevo estado de disponibilidad.</param>
    public async Task UpdateAvailabilityAsync(string id, bool isAvailable)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
        var update = Builders<Product>.Update.Set(p => p.IsAvailable, isAvailable);
        
        // UpdateOneAsync es más eficiente que reemplazar todo el documento
        await _context.Products.UpdateOneAsync(filter, update);
    }
     
    /// <summary>
    /// Busca un producto por su identificador único (ObjectId).
    /// </summary>
    public async Task<Product?> GetByIdAsync(string id)
    {
        return await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Reemplaza toda la información de un producto (Nombre, Precio, Descripción).
    /// </summary>
    public async Task UpdateAsync(string id, Product product)
    {
        await _context.Products.ReplaceOneAsync(p => p.Id == id, product);
    }

    /// <summary>
    /// Elimina físicamente un producto de la colección 'Products'.
    /// </summary>
    public async Task DeleteAsync(string id)
    {
       await _context.Products.DeleteOneAsync(p => p.Id == id);
    }
}