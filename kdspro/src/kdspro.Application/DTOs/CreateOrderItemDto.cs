namespace kdspro.Application.DTOs;

public class CreateOrderItemDto
{
    public string ProductId { get; set; } = "";

    public string ProductName { get; set; } = "";

    public int Quantity { get; set; }

    public List<string>? Modifiers { get; set; }

    public string? Notes { get; set; }
}
