using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using kdspro.Api.Services;

namespace kdspro.Api.Hubs;

[Authorize]
public class OrdersHub : Hub
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        KitchenGroup,
        WaiterGroup,
        HostGroup,
        AdminGroup,
        CashierGroup,
    };

    private const string KitchenGroup = "kitchen";
    private const string WaiterGroup = "waiter";
    private const string HostGroup = "host";
    private const string AdminGroup = "admin";
    private const string CashierGroup = "cashier";

    private readonly PresenceTracker _presenceTracker;

    public OrdersHub(PresenceTracker presenceTracker)
    {
        _presenceTracker = presenceTracker;
    }

    public async Task RegisterPresence(string browser, string? userAgent = null)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var username = Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var role = Context.User?.FindFirstValue(ClaimTypes.Role)?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role) || !AllowedRoles.Contains(role))
        {
            return;
        }

        _presenceTracker.Upsert(Context.ConnectionId, userId, username, role, browser, userAgent ?? string.Empty);

        await Clients.Group(HostGroup).SendAsync("presenceupdated");
        await Clients.Group(AdminGroup).SendAsync("presenceupdated");
    }

    public Task HeartbeatPresence()
    {
        _presenceTracker.Heartbeat(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(role) || !AllowedRoles.Contains(role))
        {
            Context.Abort();
            return;
        }

        if (role == "kitchen")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, KitchenGroup);
        }
        else if (role == "waiter")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, WaiterGroup);
        }
        else if (role == "host")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, HostGroup);
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
        _presenceTracker.Remove(Context.ConnectionId);

        // Notificar a los administradores y hosts que la presencia cambió
        await Clients.Group(HostGroup).SendAsync("presenceupdated");
        await Clients.Group(AdminGroup).SendAsync("presenceupdated");

        Console.WriteLine($"🔴 Cliente desconectado: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
