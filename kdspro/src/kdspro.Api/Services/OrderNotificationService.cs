using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Application.DTOs;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Services;

public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrdersHub> _hubContext;
    private static readonly string[] KitchenOrderGroups = ["kitchen", "admin"];
    private static readonly string[] BackOfficeGroups = ["admin", "cashier"];
    private static readonly string[] TableStateGroups = ["waiter", "host", "admin", "cashier"];

    public OrderNotificationService(IHubContext<OrdersHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewOrder(OrderDto order)
    {
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("receiveorder", order);
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("OrderCreated", order);
    }

    public async Task NotifyOrderPreparing(OrderDto order)
    {
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("orderpreparing", order);
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("OrderPreparing", order);
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("UpdateOrderStatus", new
        {
            orderId = order.Id,
            status = "Preparing",
            tableNumber = order.TableNumber,
        });
    }

    public async Task NotifyOrderReady(OrderDto order)
    {
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("orderready", order);
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("OrderReady", order);
        await SendToWaiter(order.WaiterId, "orderready", order);
        await SendToWaiter(order.WaiterId, "OrderReady", order);
        await _hubContext.Clients.Groups(KitchenOrderGroups).SendAsync("UpdateOrderStatus", new
        {
            orderId = order.Id,
            status = "Ready",
            tableNumber = order.TableNumber,
        });
        await SendToWaiter(order.WaiterId, "OrderReadyForPickup", new
        {
            orderId = order.Id,
            tableNumber = order.TableNumber,
            waiterId = order.WaiterId,
            waiterName = order.WaiterName,
            customerName = order.CustomerName,
            takeoutDestination = order.TakeoutDestination,
        });
    }

    public async Task NotifyOrderDelivered(OrderDto order)
    {
        await _hubContext.Clients.Groups(BackOfficeGroups).SendAsync("orderdelivered", order.Id);
        await _hubContext.Clients.Groups(BackOfficeGroups).SendAsync("OrderDelivered", order);
        await SendToWaiter(order.WaiterId, "orderdelivered", order.Id);
        await SendToWaiter(order.WaiterId, "OrderDelivered", order);
    }

    public async Task NotifyOrderPaid(OrderDto order)
    {
        await _hubContext.Clients.Groups(BackOfficeGroups).SendAsync("orderpaid", order);
        await _hubContext.Clients.Groups(BackOfficeGroups).SendAsync("OrderPaid", order);
        await SendToWaiter(order.WaiterId, "orderpaid", order);
        await SendToWaiter(order.WaiterId, "OrderPaid", order);
    }

    public async Task NotifyOrderCancelled(OrderDto order)
    {
        await _hubContext.Clients.Groups("kitchen", "admin", "cashier").SendAsync("ordercancelled", order.Id);
        await _hubContext.Clients.Groups("kitchen", "admin", "cashier").SendAsync("OrderCancelled", order);
        await SendToWaiter(order.WaiterId, "ordercancelled", order.Id);
        await SendToWaiter(order.WaiterId, "OrderCancelled", order);
    }

    public async Task NotifyStockUpdated(string productId, int newStock)
    {
        await _hubContext.Clients.Groups("waiter", "admin").SendAsync("stockupdated", productId, newStock);
        await _hubContext.Clients.Groups("waiter", "admin").SendAsync("StockUpdated", productId, newStock);

        if (newStock <= 0)
        {
            await _hubContext.Clients.Groups("waiter", "admin").SendAsync("productoutofstock", productId);
        }
    }

    public async Task NotifyProductOutOfStock(string productId)
    {
        await _hubContext.Clients.Groups("waiter", "admin").SendAsync("productoutofstock", productId);
    }

    public async Task NotifyTableStatusUpdated(Table table)
    {
        await _hubContext.Clients.Groups(TableStateGroups).SendAsync("tablesupdated", table);
        await _hubContext.Clients.Groups(TableStateGroups).SendAsync("TableUpdated", table);
    }

    private async Task SendToWaiter(string? waiterId, string eventName, object payload)
    {
        if (string.IsNullOrWhiteSpace(waiterId)) return;
        await _hubContext.Clients.User(waiterId).SendAsync(eventName, payload);
    }
}
