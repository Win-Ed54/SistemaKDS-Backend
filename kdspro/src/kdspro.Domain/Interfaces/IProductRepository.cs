using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task CreateAsync(Product product);
}