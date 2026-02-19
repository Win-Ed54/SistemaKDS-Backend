using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Hubs;

public class OrdersHub : Hub
{
    // Método para unir a los clientes al grupo "cocina"
    public async Task JoinKitchenGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "cocina");
    }

    public async Task SendOrderToKitchen(object order)
    {
        //Envia el objeto order solo a los miembros del grupo cocina
        await Clients.Group("cocina").SendAsync("ReceiveOrder", order);
    }

    public async Task OrderReady(string orderId)
    {
        //Notifica a todos (o a un grupo "meseros") que el pedido X esta listo
        await Clients.All.SendAsync("UpdateOrderStatus", orderId, "Listo");
    }
}

