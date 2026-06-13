using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class UpdateProductRecipeDto
{
    [Required(ErrorMessage = "Debes enviar la receta.")]
    public List<ProductRecipeItemDto> Items { get; set; } = new();
}
