using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class IngredientDto
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 80 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La unidad es obligatoria.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "La unidad debe tener entre 1 y 20 caracteres.")]
    public string Unit { get; set; } = "unidad";

    [Range(0, 999999, ErrorMessage = "El stock debe estar entre 0 y 999999.")]
    public decimal Stock { get; set; }

    [Range(0, 999999, ErrorMessage = "El stock minimo debe estar entre 0 y 999999.")]
    public decimal MinimumStock { get; set; }

    public bool IsActive { get; set; } = true;
}
