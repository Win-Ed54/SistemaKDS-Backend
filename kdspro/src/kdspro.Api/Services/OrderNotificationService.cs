using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Domain.Interfaces;
using kdspro.Domain.Entities;

namespace kdspro.Api.Services;

public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrdersHub> _hubContext;

    public OrderNotificationService(IHubContext<OrdersHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewOrder(Order order)
    {
        // Enviamos el objeto completo al grupo de cocina en minúsculas (camelCase)
        await _hubContext.Clients.Group("kitchen").SendAsync("receiveorder", order);
    }

    public async Task NotifyProductOutOfStock(string productId)
    {
        // Enviamos alerta de stock a todos
        await _hubContext.Clients.All.SendAsync("productoutofstock", productId);
    }
}
