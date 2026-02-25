using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Hubs;

/// <summary>
/// Hub de SignalR para la orquestación en tiempo real (Módulo KDS & Meseros).
/// Gestiona la comunicación bidireccional sin necesidad de "Refrescar" (F5).
/// </summary>
public class OrdersHub : Hub
{
    // --- 1. GESTIÓN DE GRUPOS ---

    /// <summary>
    /// Une al cliente al grupo de Cocina. 
    /// Las pantallas de cocina escuchan aquí para recibir nuevos tickets (ReceiveOrder).
    /// </summary>
    public async Task JoinKitchenGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "cocina");
    }

    /// <summary>
    /// NUEVO: Une al cliente al grupo de Meseros.
    /// Las tablets de los meseros escuchan aquí para saber cuándo recoger pedidos (NotifyWaiterOrderReady).
    /// </summary>
    public async Task JoinWaiterGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "waiters");
    }

    // --- 2. NOTIFICACIONES DE COCINA (INPUT) ---

    /// <summary>
    /// Envía el ticket completo a la cocina. 
    /// Se dispara cuando el mesero hace el POST de la orden (Punto 1 del Flujo Crítico).
    /// </summary>
    public async Task SendOrderToKitchen(object order)
    {
        await Clients.Group("cocina").SendAsync("ReceiveOrder", order);
    }

    // --- 3. NOTIFICACIONES DE SERVICIO (OUTPUT) ---

    /// <summary>
    /// Notifica a la cocina sobre cambios de estado internos (ej. de Pending a Preparing).
    /// </summary>
    public async Task UpdateOrderStatus(string orderId, string newStatus)
    {
        await Clients.Group("cocina").SendAsync("UpdateOrderStatus", new 
        { 
            orderId, 
            status = newStatus,
            timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// NUEVO: Notifica exclusivamente a los MESEROS que una orden está lista.
    /// Cumple el Punto 5 del Flujo Crítico: "Mesero recibe notificación".
    /// </summary>
    public async Task NotifyWaiterOrderReady(string orderId, int tableNumber, string customerName)
    {
        await Clients.Group("waiters").SendAsync("OrderReadyForPickup", new 
        { 
            orderId, 
            tableNumber,
            customerName,
            message = "¡Orden lista para servir!"
        });
    }
}
