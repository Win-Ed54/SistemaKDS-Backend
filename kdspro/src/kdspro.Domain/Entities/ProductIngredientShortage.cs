namespace kdspro.Domain.Entities;

public class ProductIngredientShortage
{
    public string IngredientId { get; set; } = string.Empty;

    public string IngredientName { get; set; } = string.Empty;

    public string Unit { get; set; } = "unidad";

    public decimal Required { get; set; }

    public decimal Available { get; set; }
}
