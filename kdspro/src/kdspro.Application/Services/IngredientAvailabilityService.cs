using kdspro.Domain.Entities;

namespace kdspro.Application.Services;

public static class IngredientAvailabilityService
{
    public static void ApplyAvailability(Product product, IReadOnlyDictionary<string, Ingredient> ingredientsById)
    {
        if (product == null) return;

        var shortages = GetShortages(product, ingredientsById, 1);
        product.IngredientShortages = shortages;
        product.IsBlockedByIngredients = shortages.Count > 0;
    }

    public static List<ProductIngredientShortage> GetShortages(
        Product product,
        IReadOnlyDictionary<string, Ingredient> ingredientsById,
        int productQuantity)
    {
        var shortages = new List<ProductIngredientShortage>();
        if (product == null || productQuantity <= 0) return shortages;

        foreach (var recipeItem in product.Recipe ?? [])
        {
            var ingredientId = recipeItem.IngredientId ?? string.Empty;
            ingredientsById.TryGetValue(ingredientId, out var ingredient);

            var required = recipeItem.QuantityRequired * productQuantity;
            var available = ingredient?.IsActive == true ? ingredient.Stock : 0;

            if (ingredient == null || !ingredient.IsActive || available < required)
            {
                shortages.Add(new ProductIngredientShortage
                {
                    IngredientId = ingredientId,
                    IngredientName = string.IsNullOrWhiteSpace(recipeItem.IngredientName)
                        ? ingredient?.Name ?? "Ingrediente"
                        : recipeItem.IngredientName,
                    Unit = string.IsNullOrWhiteSpace(recipeItem.Unit)
                        ? ingredient?.Unit ?? "unidad"
                        : recipeItem.Unit,
                    Required = required,
                    Available = Math.Max(0, available),
                });
            }
        }

        return shortages;
    }
}
