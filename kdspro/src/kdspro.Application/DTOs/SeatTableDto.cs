using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;

public class SeatTableDto
{
    [Range(1, 50, ErrorMessage = "Tamaño grupo debe estar entre 1 y 50 personas")]
    public int PartySize { get; set; }

    [Range(5, 480, ErrorMessage = "Tiempo estimado debe estar entre 5 y 480 minutos")]
    public int EstimatedDiningMinutes { get; set; }

    [StringLength(200, ErrorMessage = "Notas no pueden exceder 200 caracteres")]
    public string Notes { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "ID mesero no válido")]
    public string AssignedWaiterId { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Nombre mesero no puede exceder 50 caracteres")]
    public string AssignedWaiterName { get; set; } = string.Empty;
}
