using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Api.Services;
using kdspro.Application.DTOs;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using System.Security.Cryptography;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private static readonly HashSet<string> ProtectedManagerUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "gerente",
        "supervisor",
    };

    private readonly IUserRepository _users;
    private readonly PresenceTracker _presenceTracker;
    private readonly IHubContext<OrdersHub> _hubContext;
    private readonly AuthService _authService;

    public UsersController(
        IUserRepository users,
        PresenceTracker presenceTracker,
        IHubContext<OrdersHub> hubContext,
        AuthService authService)
    {
        _users = users;
        _presenceTracker = presenceTracker;
        _hubContext = hubContext;
        _authService = authService;
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
                    fullName = user.FullName,
                    email = user.Email,
                    role = user.Role,
                    serviceScope = string.IsNullOrWhiteSpace(user.ServiceScope) ? "hybrid" : user.ServiceScope.Trim().ToLowerInvariant(),
                    isActive = user.IsActive,
                    isDemoAccount = user.IsDemoAccount,
                    mustChangePassword = user.MustChangePassword,
                    isProtectedManager = IsProtectedManager(user),
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
                    fullName = user.FullName,
                    email = user.Email,
                    role = string.IsNullOrWhiteSpace(user.Role) ? string.Empty : user.Role.Trim().ToLowerInvariant(),
                    serviceScope = string.IsNullOrWhiteSpace(user.ServiceScope) ? "hybrid" : user.ServiceScope.Trim().ToLowerInvariant(),
                    isActive = user.IsActive,
                    isDemoAccount = user.IsDemoAccount,
                    mustChangePassword = user.MustChangePassword,
                    isProtectedManager = IsProtectedManager(user),
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt,
                    isConnected = presence != null,
                    browser = presence?.Browser ?? "Desconocido",
                    lastSeenAt = presence?.LastSeenAt,
                };
            }));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "Debes enviar la informacion del usuario." });

        var isDemoAccount = dto.IsDemoAccount;

        var normalizedUsername = string.IsNullOrWhiteSpace(dto.Username)
            ? string.Empty
            : dto.Username.Trim();
        var normalizedRole = string.IsNullOrWhiteSpace(dto.Role)
            ? string.Empty
            : dto.Role.Trim().ToLowerInvariant();
        var normalizedScope = string.IsNullOrWhiteSpace(dto.ServiceScope)
            ? "hybrid"
            : dto.ServiceScope.Trim().ToLowerInvariant();
        var normalizedEmail = string.IsNullOrWhiteSpace(dto.Email)
            ? string.Empty
            : dto.Email.Trim().ToLowerInvariant();
        var normalizedFullName = string.IsNullOrWhiteSpace(dto.FullName)
            ? normalizedUsername
            : dto.FullName.Trim();

        if (normalizedUsername.Length < 3)
            return BadRequest(new { message = "El usuario debe tener al menos 3 caracteres." });

        if (normalizedRole is not ("admin" or "cashier" or "host" or "kitchen" or "waiter"))
            return BadRequest(new { message = "El rol indicado no es valido." });

        if (normalizedScope is not ("dining" or "takeout" or "hybrid"))
            return BadRequest(new { message = "El alcance de servicio no es valido." });

        if (normalizedRole != "waiter")
        {
            normalizedScope = "hybrid";
        }

        var existingUser = await _users.GetByUsername(normalizedUsername);
        if (existingUser != null)
            return Conflict(new { message = "Ya existe un usuario con ese nombre." });

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var existingEmail = await _users.GetByEmail(normalizedEmail);
            if (existingEmail != null)
                return Conflict(new { message = "Ya existe un usuario con ese correo." });
        }

        if (isDemoAccount && normalizedRole == "waiter")
        {
            normalizedScope = "hybrid";
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var user = new User
        {
            Username = normalizedUsername,
            FullName = normalizedFullName,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            Role = normalizedRole,
            ServiceScope = normalizedScope,
            IsActive = true,
            MustChangePassword = !isDemoAccount,
            IsDemoAccount = isDemoAccount,
            CreatedAt = DateTime.UtcNow,
        };

        await _users.Create(user);

        return Ok(new
        {
            id = user.Id,
            username = user.Username,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role,
            serviceScope = user.ServiceScope,
            isDemoAccount = user.IsDemoAccount,
            temporaryPassword = isDemoAccount ? string.Empty : temporaryPassword,
            requiresPasswordChange = user.MustChangePassword,
        });
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateUserStatusDto dto)
    {
        var user = await _users.GetById(id);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });
        if (IsProtectedManager(user))
            return BadRequest(new { message = "Las cuentas de gerencia no pueden desactivarse ni reactivarse desde este panel." });

        await _users.UpdateActiveState(id, dto.IsActive);

        if (!dto.IsActive)
        {
            await _users.UpdateCurrentSessionId(id, string.Empty);
            await _authService.RevokeAllRefreshTokens(id);
            await NotifyUserSessionRevoked(id, "account_deactivated");
        }

        await NotifyServiceScopeChanges(new Dictionary<string, string>());

        return Ok(new
        {
            id,
            isActive = dto.IsActive,
        });
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _users.GetById(id);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });
        if (IsProtectedManager(user))
            return BadRequest(new { message = "Las cuentas de gerencia no permiten generar contrasenas temporales desde este panel." });

        var temporaryPassword = GenerateTemporaryPassword();
        await _users.UpdatePasswordState(id, BCrypt.Net.BCrypt.HashPassword(temporaryPassword), true);
        await _users.UpdateCurrentSessionId(id, string.Empty);
        await _authService.RevokeAllRefreshTokens(id);
        await NotifyUserSessionRevoked(id, "password_reset");
        await NotifyServiceScopeChanges(new Dictionary<string, string>());

        return Ok(new
        {
            id,
            username = user.Username,
            temporaryPassword,
            requiresPasswordChange = true,
        });
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

        var waiters = await _users.GetByRole("waiter");
        var existingTakeoutWaiter = waiters.FirstOrDefault(waiter =>
            string.Equals(waiter?.ServiceScope, "takeout", StringComparison.OrdinalIgnoreCase));

        var affectedUsers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (normalizedScope == "takeout")
        {
            foreach (var waiter in waiters)
            {
                if (string.IsNullOrWhiteSpace(waiter.Id) || string.Equals(waiter.Id, id, StringComparison.OrdinalIgnoreCase))
                    continue;

                await _users.UpdateServiceScope(waiter.Id, "dining");
                affectedUsers[waiter.Id] = "dining";
            }
        }
        else if (normalizedScope == "hybrid" && existingTakeoutWaiter != null)
        {
            if (!string.Equals(existingTakeoutWaiter.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                normalizedScope = "dining";
            }
        }

        affectedUsers[id] = normalizedScope;
        await _users.UpdateServiceScope(id, normalizedScope);

        foreach (var waiter in waiters)
        {
            if (string.IsNullOrWhiteSpace(waiter.Id) || string.Equals(waiter.Id, id, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!affectedUsers.ContainsKey(waiter.Id) &&
                string.Equals(waiter.ServiceScope, "takeout", StringComparison.OrdinalIgnoreCase))
            {
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
        await _hubContext.Clients.Groups("admin", "host", "waiter").SendAsync("staffupdated");
        await _hubContext.Clients.Groups("admin", "host", "waiter").SendAsync("StaffUpdated");

        foreach (var entry in affectedUsers)
        {
            if (string.IsNullOrWhiteSpace(entry.Key)) continue;

            var payload = new
            {
                userId = entry.Key,
                serviceScope = entry.Value,
            };

            await _hubContext.Clients.Group("waiter").SendAsync("servicescopeupdated", payload);
            await _hubContext.Clients.Group("waiter").SendAsync("ServiceScopeUpdated", payload);
            await _hubContext.Clients.User(entry.Key).SendAsync("servicescopeupdated", payload);
            await _hubContext.Clients.User(entry.Key).SendAsync("ServiceScopeUpdated", payload);
        }
    }

    private async Task NotifyUserSessionRevoked(string userId, string reason)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var payload = new
        {
            userId,
            reason,
        };

        await _hubContext.Clients.User(userId).SendAsync("sessionrevoked", payload);
        await _hubContext.Clients.User(userId).SendAsync("SessionRevoked", payload);
    }

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        var bytes = RandomNumberGenerator.GetBytes(12);
        var chars = new char[12];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }

    private static bool IsProtectedManager(User? user)
    {
        if (user == null) return false;

        var normalizedRole = string.IsNullOrWhiteSpace(user.Role)
            ? string.Empty
            : user.Role.Trim().ToLowerInvariant();
        var normalizedUsername = string.IsNullOrWhiteSpace(user.Username)
            ? string.Empty
            : user.Username.Trim();

        return normalizedRole == "admin" && ProtectedManagerUsernames.Contains(normalizedUsername);
    }
}
