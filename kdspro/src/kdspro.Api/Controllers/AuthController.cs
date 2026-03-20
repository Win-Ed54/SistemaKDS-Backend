using Microsoft.AspNetCore.Mvc;
using kdspro.Application.DTOs;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;
using kdspro.Api.Services;

namespace kdspro.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService    _auth;
    private readonly MongoDbContext _context;

    public AuthController(AuthService auth, MongoDbContext context)
    {
        _auth    = auth;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _auth.Login(dto.Username, dto.Password);
        if (result.token == null)
            return Unauthorized("Credenciales incorrectas");

        var refreshToken = await _auth.GenerateRefreshToken(result.userId!);

        return Ok(new
        {
            token        = result.token,
            role         = result.role,
            refreshToken,
            expiresIn    = 43200
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var result = await _auth.RefreshToken(dto.RefreshToken);
        if (result == null)
            return Unauthorized("Refresh token inválido o expirado");

        return Ok(new
        {
            token        = result.Token,
            role         = result.Role,
            refreshToken = result.NewRefreshToken,
            expiresIn    = 43200
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        await _auth.RevokeRefreshToken(dto.RefreshToken);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpGet("test-mongo")]
    public async Task<IActionResult> TestMongo()
    {
        var users = await _context.Users.Find(_ => true).ToListAsync();
        return Ok(users.Count);
    }
}