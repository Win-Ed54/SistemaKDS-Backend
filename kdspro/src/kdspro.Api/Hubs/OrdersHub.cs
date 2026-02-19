using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Hubs;

public class OrdersHub : Hub
{
    // 1. Registro de Clientes (Pantallas de Cocina y Tablets de Meseros)
    // Al conectar, todos entran al grupo general "cocina"
    public async Task JoinKitchenGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "cocina");
    }

    // 2. Notificación de Nuevo Pedido (Evento: ReceiveOrder)
    // Se invoca desde tu Controller/Service después de guardar en MongoDB.
    // Esto hace que el "Ticket" aparezca instantáneamente en la pantalla de cocina.
    public async Task SendOrderToKitchen(object order)
    {
        await Clients.Group("cocina").SendAsync("ReceiveOrder", order);
    }

    // 3. Notificación de Cambio de Estado (Evento: UpdateOrderStatus)
    // Cuando el cocinero marca "Preparando" o "Listo", el sistema avisa a todos.
    // Esto permite que el Mesero reciba la notificación en su móvil (Flujo Crítico 5).
    public async Task UpdateOrderStatus(string orderId, string newStatus)
    {
        await Clients.Group("cocina").SendAsync("UpdateOrderStatus", new 
        { 
            orderId, 
            status = newStatus,
            timestamp = DateTime.UtcNow 
        });
    }
}