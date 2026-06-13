using kdspro.Api.Services;
using kdspro.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _auth.Login(dto.Username, dto.Password);
        if (result.isSessionBlocked)
            return Conflict(new { message = result.errorMessage });

        if (result.token == null)
            return Unauthorized(new { message = "No se pudo iniciar sesion." });

        var refreshToken = await _auth.GenerateRefreshToken(result.userId!);

        return Ok(new
        {
            token = result.token,
            role = result.role,
            serviceScope = result.serviceScope,
            requiresPasswordChange = result.requiresPasswordChange,
            refreshToken,
            expiresIn = 43200
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _auth.RefreshToken(dto.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "La sesion no es valida o expiro." });

        return Ok(new
        {
            token = result.Token,
            role = result.Role,
            serviceScope = result.ServiceScope,
            refreshToken = result.NewRefreshToken,
            expiresIn = 43200
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auth.Logout(userId ?? string.Empty, dto.RefreshToken);
        return NoContent();
    }

    [HttpPost("recover-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordDto dto)
    {
        var recovered = await _auth.RecoverPassword(
            dto.Username,
            dto.NewPassword,
            dto.RecoveryKey);

        if (!recovered)
            return Unauthorized(new { message = "No se pudo recuperar la cuenta." });

        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "No se pudo identificar la sesion." });

        var changed = await _auth.ChangePassword(userId, dto.CurrentPassword, dto.NewPassword);
        if (!changed)
            return BadRequest(new { message = "No se pudo actualizar la contrasena." });

        return NoContent();
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            serverTime = DateTime.UtcNow
        });
    }
}
