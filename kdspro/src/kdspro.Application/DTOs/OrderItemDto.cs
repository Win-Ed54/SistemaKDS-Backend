using System.Collections.Generic;

namespace kdspro.Application.DTOs;

public class OrderItemDto
{
    public int LineIndex { get; set; }
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public int PaidQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public List<string>? Modifiers { get; set; }
    public string? Notes { get; set; }
    public int CurrentStock { get; set; }
}
