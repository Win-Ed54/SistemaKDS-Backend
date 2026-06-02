using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Api.Services;
using kdspro.Application.DTOs;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly PresenceTracker _presenceTracker;
    private readonly IHubContext<OrdersHub> _hubContext;

    public UsersController(
        IUserRepository users,
        PresenceTracker presenceTracker,
        IHubContext<OrdersHub> hubContext)
    {
        _users = users;
        _presenceTracker = presenceTracker;
        _hubContext = hubContext;
    }

    [HttpGet("waiters")]
    [Authorize(Roles = "host,admin")]
    public async Task<IActionResult> GetWaiters()
    {
        var waiters = await _users.GetByRole("waiter");
        var presenceMap = _presenceTracker.GetCurrentPresence();

        return Ok(waiters
            .Where(user =>
            {
                var serviceScope = string.IsNullOrWhiteSpace(user.ServiceScope)
                    ? "hybrid"
                    : user.ServiceScope.Trim().ToLowerInvariant();

                return serviceScope != "takeout";
            })
            .Select(user =>
            {
                var presence = presenceMap.TryGetValue(user.Id, out var current) ? current : null;

                return new
                {
                    id = user.Id,
                    username = user.Username,
                    role = user.Role,
                    serviceScope = string.IsNullOrWhiteSpace(user.ServiceScope) ? "hybrid" : user.ServiceScope.Trim().ToLowerInvariant(),
                    isConnected = presence != null,
                    browser = presence?.Browser ?? "Desconocido",
                    lastSeenAt = presence?.LastSeenAt,
                };
            }));
    }

    [HttpGet("staff")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetStaff()
    {
        var users = await _users.GetAll();
        var presenceMap = _presenceTracker.GetCurrentPresence();

        return Ok(users
            .OrderBy(user => user.Role)
            .ThenBy(user => user.Username)
            .Select(user =>
            {
                var presence = presenceMap.TryGetValue(user.Id, out var current) ? current : null;

                return new
                {
                    id = user.Id,
                    username = user.Username,
                    role = string.IsNullOrWhiteSpace(user.Role) ? string.Empty : user.Role.Trim().ToLowerInvariant(),
                    serviceScope = string.IsNullOrWhiteSpace(user.ServiceScope) ? "hybrid" : user.ServiceScope.Trim().ToLowerInvariant(),
                    isConnected = presence != null,
                    browser = presence?.Browser ?? "Desconocido",
                    lastSeenAt = presence?.LastSeenAt,
                };
            }));
    }

    [HttpPatch("{id}/service-scope")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateServiceScope(string id, [FromBody] UpdateUserServiceScopeDto dto)
    {
        var user = await _users.GetById(id);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });

        var normalizedRole = string.IsNullOrWhiteSpace(user.Role) ? string.Empty : user.Role.Trim().ToLowerInvariant();
        if (normalizedRole != "waiter")
            return BadRequest(new { message = "Solo los meseros pueden cambiar su alcance de servicio." });

        var normalizedScope = string.IsNullOrWhiteSpace(dto?.ServiceScope)
            ? "hybrid"
            : dto.ServiceScope.Trim().ToLowerInvariant();

        if (normalizedScope is not ("dining" or "takeout" or "hybrid"))
            return BadRequest(new { message = "El alcance debe ser solo mesas, solo para llevar o mixto." });

        var affectedUsers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [id] = normalizedScope
        };

        await _users.UpdateServiceScope(id, normalizedScope);

        if (normalizedScope == "takeout")
        {
            var waiters = await _users.GetByRole("waiter");
            var otherWaiters = waiters.Where(waiter => !string.Equals(waiter.Id, id, StringComparison.OrdinalIgnoreCase));

            foreach (var waiter in otherWaiters)
            {
                if (string.IsNullOrWhiteSpace(waiter.Id)) continue;

                await _users.UpdateServiceScope(waiter.Id, "dining");
                affectedUsers[waiter.Id] = "dining";
            }
        }

        await NotifyServiceScopeChanges(affectedUsers);

        return Ok(new
        {
            id,
            serviceScope = normalizedScope,
            affectedUsers = affectedUsers.Select(entry => new { id = entry.Key, serviceScope = entry.Value }),
        });
    }

    private async Task NotifyServiceScopeChanges(IReadOnlyDictionary<string, string> affectedUsers)
    {
        await _hubContext.Clients.Groups("admin", "host").SendAsync("staffupdated");
        await _hubContext.Clients.Groups("admin", "host").SendAsync("StaffUpdated");

        foreach (var entry in affectedUsers)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)) continue;

            var payload = new
            {
                userId = entry.Key,
                serviceScope = entry.Value,
            };

            await _hubContext.Clients.User(entry.Key).SendAsync("servicescopeupdated", payload);
            await _hubContext.Clients.User(entry.Key).SendAsync("ServiceScopeUpdated", payload);
        }
    }
}
