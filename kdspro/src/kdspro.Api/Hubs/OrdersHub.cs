using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Hubs;

public class OrdersHub : Hub
{
    // --- GRUPOS ---

    public async Task JoinKitchenGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "cocina");
    }

    public async Task JoinWaiterGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "waiters");
    }

    // --- BROADCAST GENERAL ---

    public async Task BroadcastOrderStatus(string orderId, string status)
    {
        await Clients.All.SendAsync("OrderStatusChanged", new
        {
            orderId,
            status,
            timestamp = DateTime.UtcNow
        });
    }

    // --- NOTIFICACIÓN A MESEROS ---

    public async Task NotifyWaiterOrderReady(string orderId, int tableNumber, string customerName)
    {
        await Clients.Group("waiters").SendAsync("NotifyWaiterOrderReady", new
        {
            orderId,
            tableNumber,
            customerName
        });
    }
}
