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
    private const string CashierGroup = "cashier";

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "kitchen")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, KitchenGroup);
        }
        else if (role == "waiter")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, WaiterGroup);
        }
        else if (role == "admin")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }
        else if (role == "cashier")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CashierGroup);
        }

        Console.WriteLine($"🟢 Cliente conectado: {Context.ConnectionId} | Rol: {role}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"🔴 Cliente desconectado: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
