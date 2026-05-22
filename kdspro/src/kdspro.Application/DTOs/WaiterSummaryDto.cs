namespace kdspro.Application.DTOs;

public class WaiterSummaryDto
{
    public string WaiterId { get; set; } = string.Empty;
    public string WaiterName { get; set; } = string.Empty;
    public int TotalCreated { get; set; }
    public int TotalDelivered { get; set; }
    public bool HasDedicatedTakeoutWaiter { get; set; }
    public List<OrderDto> MyActiveOrders { get; set; } = new();
    public List<OrderDto> PendingCleanupOrders { get; set; } = new();
}
