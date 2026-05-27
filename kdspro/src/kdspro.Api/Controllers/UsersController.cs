using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public UsersController(IUserRepository users, PresenceTracker presenceTracker)
    {
        _users = users;
        _presenceTracker = presenceTracker;
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

        await _users.UpdateServiceScope(id, normalizedScope);
        return Ok(new { id, serviceScope = normalizedScope });
    }
}
