using System;
using System.Collections.Generic;
using kdspro.Domain.Enums;

namespace kdspro.Application.DTOs;

public class OrderDto
{
    public string Id { get; set; } = "";
    public int TableNumber { get; set; }
    public string CustomerName { get; set; } = "";
    public string WaiterName { get; set; } = "";
    public OrderStatus Status { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsCleanupCompleted { get; set; }
    public DateTime? CleanupCompletedAt { get; set; }
}
