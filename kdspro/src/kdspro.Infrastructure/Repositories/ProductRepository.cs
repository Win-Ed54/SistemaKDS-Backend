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
}