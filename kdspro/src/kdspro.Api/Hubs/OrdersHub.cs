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
        if (!TryGetPresenceIdentity(out var userId, out var username, out var role))
        {
            return;
        }

        _presenceTracker.Upsert(Context.ConnectionId, userId, username, role, browser, userAgent ?? string.Empty);

        await Clients.Group(HostGroup).SendAsync("presenceupdated");
        await Clients.Group(AdminGroup).SendAsync("presenceupdated");
    }

    public Task HeartbeatPresence()
    {
        if (_presenceTracker.GetByConnectionId(Context.ConnectionId) == null &&
            TryGetPresenceIdentity(out var userId, out var username, out var role))
        {
            _presenceTracker.Upsert(Context.ConnectionId, userId, username, role, "Unknown", string.Empty);
        }

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

        if (TryGetPresenceIdentity(out var userId, out var username, out var normalizedRole))
        {
            // Registrar presencia basica al conectar evita falsos "desconectado"
            // si RegisterPresence falla o llega mas tarde desde el cliente.
            _presenceTracker.Upsert(Context.ConnectionId, userId, username, normalizedRole, "Unknown", string.Empty);
            await Clients.Group(HostGroup).SendAsync("presenceupdated");
            await Clients.Group(AdminGroup).SendAsync("presenceupdated");
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

    private bool TryGetPresenceIdentity(out string userId, out string username, out string role)
    {
        userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        username = Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        role = Context.User?.FindFirstValue(ClaimTypes.Role)?.Trim().ToLowerInvariant() ?? string.Empty;

        return
            !string.IsNullOrWhiteSpace(userId) &&
            !string.IsNullOrWhiteSpace(role) &&
            AllowedRoles.Contains(role);
    }
}
