using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(string id); // Útil para validaciones antes de crear órdenes
    Task CreateAsync(Product product);
    
    // REQUERIMIENTO MES 1: Activar/Desactivar platillos para stock básico
    Task UpdateAvailabilityAsync(string id, bool isAvailable);
    
    // Opcional pero recomendado para un CRUD completo
    Task UpdateAsync(string id, Product product);
    Task DeleteAsync(string id);
}