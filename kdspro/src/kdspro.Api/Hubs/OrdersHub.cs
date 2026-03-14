using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace kdspro.Api.Hubs;

[Authorize]
public class OrdersHub : Hub
{
    private const string KitchenGroup = "kitchen";
    private const string WaiterGroup = "waiter";

    // Unirse al grupo de cocina
    public async Task JoinKitchenGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, KitchenGroup);
    }

    // Unirse al grupo de meseros
    public async Task JoinWaiterGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, WaiterGroup);
    }

    // Cuando un cliente se conecta
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Cliente conectado: {Context.ConnectionId}");

        await base.OnConnectedAsync();
    }

    // Cuando un cliente se desconecta
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Cliente desconectado: {Context.ConnectionId}");

        if (exception != null)
        {
            Console.WriteLine($"Error: {exception.Message}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateStock(int productId, int newStock)
    {
        // Notifica a todos los meseros que el stock cambió
        await Clients.Group("waiter").SendAsync("stockupdated", productId, newStock);
    }
}
