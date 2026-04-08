using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Application.DTOs;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Services;

public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrdersHub> _hubContext;

    public OrderNotificationService(IHubContext<OrdersHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewOrder(OrderDto order)
    {
        await _hubContext.Clients.Groups("kitchen", "admin").SendAsync("receiveorder", order);
        await _hubContext.Clients.Groups("kitchen", "admin").SendAsync("OrderCreated", order);
    }

    public async Task NotifyOrderPreparing(OrderDto order)
    {
        await _hubContext.Clients.Group("admin").SendAsync("orderpreparing", order);
        await _hubContext.Clients.Group("admin").SendAsync("OrderPreparing", order);
    }

    public async Task NotifyOrderReady(OrderDto order)
    {
        await _hubContext.Clients.Groups("waiter", "admin").SendAsync("orderready", order);
        await _hubContext.Clients.Groups("waiter", "admin").SendAsync("OrderReady", order);
    }

    public async Task NotifyOrderDelivered(OrderDto order)
    {
        await _hubContext.Clients.All.SendAsync("orderdelivered", order.Id);
        await _hubContext.Clients.All.SendAsync("OrderDelivered", order);
    }

    public async Task NotifyOrderPaid(OrderDto order)
    {
        await _hubContext.Clients.Groups("waiter", "admin", "cashier").SendAsync("orderpaid", order);
        await _hubContext.Clients.Groups("waiter", "admin", "cashier").SendAsync("OrderPaid", order);
    }

    public async Task NotifyOrderCancelled(OrderDto order)
    {
        await _hubContext.Clients.All.SendAsync("ordercancelled", order.Id);
        await _hubContext.Clients.All.SendAsync("OrderCancelled", order);
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

    public async Task NotifyTableStatusUpdated(int tableNumber, bool isOccupied)
    {
        var payload = new { tableNumber, isOccupied };

        await _hubContext.Clients.All.SendAsync("tablesupdated", payload);
        await _hubContext.Clients.All.SendAsync("TableUpdated", payload);
    }
}
