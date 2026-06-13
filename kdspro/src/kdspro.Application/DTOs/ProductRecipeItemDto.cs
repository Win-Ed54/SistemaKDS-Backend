using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class ProductRecipeItemDto
{
    [Required(ErrorMessage = "El ingrediente es obligatorio.")]
    [StringLength(80, MinimumLength = 1, ErrorMessage = "El ingrediente no es valido.")]
    public string IngredientId { get; set; } = string.Empty;

    [Range(0.0001, 999999, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public decimal QuantityRequired { get; set; }
}
