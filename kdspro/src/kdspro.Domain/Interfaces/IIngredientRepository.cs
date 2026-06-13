using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IIngredientRepository
{
    Task<List<Ingredient>> GetAllAsync();
    Task<Ingredient?> GetByIdAsync(string id);
    Task CreateAsync(Ingredient ingredient);
    Task UpdateAsync(string id, Ingredient ingredient);
    Task DeleteAsync(string id);
    Task<bool> DeductStockAsync(string id, decimal quantity);
    Task RestoreStockAsync(string id, decimal quantity);
}
