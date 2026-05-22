using System;
using System.Collections.Generic;
using kdspro.Domain.Enums;

namespace kdspro.Application.DTOs;

public class OrderDto
{
    public string Id { get; set; } = "";
    public int TableNumber { get; set; }
    public int CorrelativeNumber { get; set; }
    public string CorrelativeCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string TakeoutDestination { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public string WaiterId { get; set; } = "";
    public string WaiterName { get; set; } = "";
    public string PreparedByName { get; set; } = "";
    public string PaidByName { get; set; } = "";
    public string CancelledByName { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string ReceiptNumber { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public bool InvoiceRequested { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
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
