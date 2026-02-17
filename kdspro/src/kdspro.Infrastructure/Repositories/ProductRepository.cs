using MongoDB.Driver;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;

namespace kdspro.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly MongoDbContext _context;

    // Aquí "inyectamos" la conexión a MongoDB que configuramos antes
    public ProductRepository(MongoDbContext context)
    {
        _context = context;
    }

    // Acción para obtener todos los platillos del menú
    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products.Find(_ => true).ToListAsync();
    }

    // Acción para guardar un nuevo platillo
    public async Task CreateAsync(Product product)
    {
        await _context.Products.InsertOneAsync(product);
    }

    public async Task UpdateAvailabilityAsync(string id, bool isAvailable)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
        var update = Builders<Product>.Update.Set(p => p.IsAvailable, isAvailable);
        await _context.Products.UpdateOneAsync(filter, update);
    }
     
     // 1. Obtener por ID
    public async Task<Product?> GetByIdAsync(string id)
    {
        return await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

     // 2. Actualización completa
    public async Task UpdateAsync(string id, Product product)
    {
        await _context.Products.ReplaceOneAsync(p => p.Id == id, product);
    }

     // 3. Eliminar
    public async Task DeleteAsync(string id)
    {
       await _context.Products.DeleteOneAsync(p => p.Id == id);
    }


    
}