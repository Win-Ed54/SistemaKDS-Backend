using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class CreateOrderItemDto
{
    [Required(ErrorMessage = "ID del producto es obligatorio")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "ID producto inválido")]
    public string ProductId { get; set; } = "";

    [Required(ErrorMessage = "Nombre del producto es obligatorio")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Nombre producto entre 1-100 caracteres")]
    public string ProductName { get; set; } = "";

    [Range(1, 999, ErrorMessage = "Cantidad debe estar entre 1 y 999")]
    public int Quantity { get; set; }

    [Range(0, 99999.99, ErrorMessage = "Precio no válido")]
    public decimal Price { get; set; }

    public List<string>? Modifiers { get; set; }

    [StringLength(160, ErrorMessage = "Notas no pueden exceder 160 caracteres")]
    public string? Notes { get; set; }
}
