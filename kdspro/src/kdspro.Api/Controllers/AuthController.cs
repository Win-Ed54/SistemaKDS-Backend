using kdspro.Api.Services;
using kdspro.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace kdspro.Api.Controllers;

[AllowAnonymous]
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
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _auth.Login(dto.Username, dto.Password);
        if (result.token == null)
            return Unauthorized(new { message = "No se pudo iniciar sesion." });

        var refreshToken = await _auth.GenerateRefreshToken(result.userId!);

        return Ok(new
        {
            token = result.token,
            role = result.role,
            refreshToken,
            expiresIn = 43200
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
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
            refreshToken = result.NewRefreshToken,
            expiresIn = 43200
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        await _auth.RevokeRefreshToken(dto.RefreshToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("recover-password")]
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

    [AllowAnonymous]
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            serverTime = DateTime.UtcNow
        });
    }
}
