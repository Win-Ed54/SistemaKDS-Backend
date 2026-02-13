using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Hubs;

public class OrdersHub : Hub
{
    // Método para unir a los clientes al grupo "cocina"
    public async Task JoinKitchenGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "cocina");
    }
}