using System.Collections.Generic;

namespace kdspro.Application.DTOs;

public class OrderItemDto
{
    public string ProductId { get; set; } = "";

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public List<string>? Modifiers { get; set; }

    public string? Notes { get; set; }

    public int CurrentStock { get; set; } // El stock que queda después de la venta

}

