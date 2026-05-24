using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace kdspro.Application.DTOs;
public class CreateOrderDto
{
    [Range(0, 99, ErrorMessage = "Numero de mesa debe estar entre 0 (para llevar) y 99")]
    public int TableNumber { get; set; }

    [StringLength(80, MinimumLength = 0, ErrorMessage = "Nombre cliente no puede exceder 80 caracteres")]
    public string CustomerName { get; set; } = "";

    [StringLength(80, MinimumLength = 0, ErrorMessage = "Destino para llevar no puede exceder 80 caracteres")]
    public string TakeoutDestination { get; set; } = "";

    [StringLength(180, MinimumLength = 0, ErrorMessage = "Direccion de delivery no puede exceder 180 caracteres")]
    public string DeliveryAddress { get; set; } = "";

    [StringLength(50, MinimumLength = 0, ErrorMessage = "Nombre mesero no puede exceder 50 caracteres")]
    public string WaiterName { get; set; } = "";

    [Required(ErrorMessage = "Debe incluir al menos un producto")]
    [MinLength(1, ErrorMessage = "Mínimo 1 producto requerido")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
