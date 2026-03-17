using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Application.Interfaces; // Asegúrate de que apunte a la nueva ruta de la interfaz
using kdspro.Application.DTOs;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Services;

public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrdersHub> _hubContext;

    public OrderNotificationService(IHubContext<OrdersHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // 1. Notificar nueva orden (Mesero -> Cocina y Admin)
    public async Task NotifyNewOrder(Order order)
    {
        // Al enviar al Admin, su Dashboard agregará la orden y marcará la mesa como ocupada
        await _hubContext.Clients.Groups("kitchen", "admin").SendAsync("receiveorder", order);
    }

    // 2. Notificar que la cocina empezó (KDS -> Admin)
    public async Task NotifyOrderPreparing(OrderDto order)
    {
        // Esto cambia el color de la orden en el Admin y registra el 'StartedAt'
        await _hubContext.Clients.Group("admin").SendAsync("orderpreparing", order);
    }

    // 3. Notificar que la orden está lista (KDS -> Mesero y Admin)
    public async Task NotifyOrderReady(OrderDto order)
    {
        // El mesero recibe la alerta sonora y el Admin calcula la eficiencia (ReadyAt - StartedAt)
        await _hubContext.Clients.All.SendAsync("orderready", order);
    }

    // 4. Notificar entrega (Mesa se libera en Admin)
    public async Task NotifyOrderDelivered(string orderId)
    {
        // Al recibir esto, el Admin ejecuta loadData() y la mesa vuelve a verde (Libre)
        await _hubContext.Clients.All.SendAsync("orderdelivered", orderId);
    }

    // 5. Notificar cancelación
    public async Task NotifyOrderCancelled(string orderId)
    {
        await _hubContext.Clients.All.SendAsync("ordercancelled", orderId);
    }

    public async Task NotifyProductOutOfStock(string productId)
    {
        await _hubContext.Clients.All.SendAsync("productoutofstock", productId);
    }
}
