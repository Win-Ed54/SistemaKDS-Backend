using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

/// <summary>
/// Interfaz para el repositorio de Productos (Módulo de Menú - Mes 1).
/// Define las operaciones para gestionar el catálogo de Wendy's y la disponibilidad de stock.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Recupera la lista completa de productos del catálogo (Hamburguesas, Pollo, Bebidas, etc.).
    /// </summary>
    Task<List<Product>> GetAllAsync();

    /// <summary>
    /// Busca un producto específico por su identificador único. 
    /// Crucial para validar precios y existencia antes de registrar una nueva orden.
    /// </summary>
    /// <param name="id">ID del producto en MongoDB.</param>
    Task<Product?> GetByIdAsync(string id);

    /// <summary>
    /// Registra un nuevo producto en la base de datos de forma atómica.
    /// </summary>
    Task CreateAsync(Product product);
    
    /// <summary>
    /// REQUISITO MES 1: Gestión de Stock Crítico. 
    /// Permite activar o desactivar platillos instantáneamente (ej: "Sin Carne").
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="isAvailable">Estado de disponibilidad (true/false).</param>
    Task UpdateAvailabilityAsync(string id, bool isAvailable);
    
    /// <summary>
    /// Actualiza la información completa de un producto (Nombre, Precio, Descripción).
    /// </summary>
    Task UpdateAsync(string id, Product product);

    /// <summary>
    /// Elimina un producto del catálogo (Uso administrativo).
    /// </summary>
    Task DeleteAsync(string id);

    Task<bool> DeductStockAsync(string id, int quantity);

    Task UpdateStockAsync(string id, int newStock);

    Task RestoreStockAsync(string productId, int quantity);

    Task UpdateRecipeAsync(string id, List<ProductRecipeItem> recipe);

}
