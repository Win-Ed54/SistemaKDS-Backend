using MongoDB.Driver;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;

namespace kdspro.Infrastructure.Repositories;

public class IngredientRepository : IIngredientRepository
{
    private readonly MongoDbContext _context;

    public IngredientRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ingredient>> GetAllAsync()
    {
        return await _context.Ingredients.Find(_ => true).ToListAsync();
    }

    public async Task<Ingredient?> GetByIdAsync(string id)
    {
        return await _context.Ingredients.Find(ingredient => ingredient.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(Ingredient ingredient)
    {
        await _context.Ingredients.InsertOneAsync(ingredient);
    }

    public async Task UpdateAsync(string id, Ingredient ingredient)
    {
        await _context.Ingredients.ReplaceOneAsync(current => current.Id == id, ingredient);
    }

    public async Task DeleteAsync(string id)
    {
        await _context.Ingredients.DeleteOneAsync(ingredient => ingredient.Id == id);
    }

    public async Task<bool> DeductStockAsync(string id, decimal quantity)
    {
        var filter = Builders<Ingredient>.Filter.And(
            Builders<Ingredient>.Filter.Eq(ingredient => ingredient.Id, id),
            Builders<Ingredient>.Filter.Eq(ingredient => ingredient.IsActive, true),
            Builders<Ingredient>.Filter.Gte(ingredient => ingredient.Stock, quantity));

        var update = Builders<Ingredient>.Update.Inc(ingredient => ingredient.Stock, -quantity);
        var result = await _context.Ingredients.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task RestoreStockAsync(string id, decimal quantity)
    {
        var filter = Builders<Ingredient>.Filter.Eq(ingredient => ingredient.Id, id);
        var update = Builders<Ingredient>.Update.Inc(ingredient => ingredient.Stock, quantity);
        await _context.Ingredients.UpdateOneAsync(filter, update);
    }
}
