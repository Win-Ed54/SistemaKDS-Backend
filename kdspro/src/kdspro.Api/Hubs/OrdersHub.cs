using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace kdspro.Api.Hubs;

[Authorize]
public class OrdersHub : Hub
{
    private const string KitchenGroup = "kitchen";
    private const string WaiterGroup = "waiter";
    private const string AdminGroup = "admin";

    // ✅ RE-UNIÓN AUTOMÁTICA AL CONECTAR
    public override async Task OnConnectedAsync()
    {
        // Extraemos el rol directamente del Token JWT del usuario
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        // Al conectar (o reconectar), el Hub lo mete automáticamente a su grupo
        if (role == "kitchen") await Groups.AddToGroupAsync(Context.ConnectionId, KitchenGroup);
        else if (role == "waiter") await Groups.AddToGroupAsync(Context.ConnectionId, WaiterGroup);
        else if (role == "admin") await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

        // Log útil para depuración en desarrollo
        Console.WriteLine($"🚀 Cliente conectado: {Context.ConnectionId} | Rol: {role?.ToUpper() ?? "SIN ROL"}");

        await base.OnConnectedAsync();
    }

    // Métodos manuales (por si el frontend necesita forzar la unión)
    public async Task JoinKitchenGroup() => await Groups.AddToGroupAsync(Context.ConnectionId, KitchenGroup);
    public async Task JoinWaiterGroup()  => await Groups.AddToGroupAsync(Context.ConnectionId, WaiterGroup);
    public async Task JoinAdminGroup()   => await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

    // ✅ SINCRONIZACIÓN DE STOCK GLOBAL
    // Cambiamos productId a string para que coincida con tus IDs de MongoDB/GUID
    public async Task UpdateStock(string productId, int newStock)
    {
        // Notificamos a meseros y admin para que el inventario sea coherente en todo el local
        await Clients.Groups(WaiterGroup, AdminGroup).SendAsync("stockupdated", productId, newStock);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            Console.WriteLine($"❌ Error en conexión {Context.ConnectionId}: {exception.Message}");
        }
        else
        {
            Console.WriteLine($"👋 Cliente desconectado: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
