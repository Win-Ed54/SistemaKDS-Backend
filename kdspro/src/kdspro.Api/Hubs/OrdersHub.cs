using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Hubs;

public class OrdersHub : Hub
{
    private const string KitchenGroup = "cocina";
    private const string WaiterGroup = "waiters";

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
}
