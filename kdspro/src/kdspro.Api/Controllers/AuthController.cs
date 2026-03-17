using Microsoft.AspNetCore.Mvc;
using kdspro.Application.Services;
using kdspro.Application.DTOs;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authorization;

[AllowAnonymous]
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly MongoDbContext _context;

    public AuthController(AuthService auth, MongoDbContext context)
    {
        _auth = auth;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _auth.Login(dto.Username, dto.Password);

        if (result.token == null)
            return Unauthorized("Credenciales incorrectas");

        return Ok(new
        {
            token = result.token,
            role = result.role
        });
    }
    [Authorize(Roles ="admin")]
    [HttpGet("test-mongo")]
    public async Task<IActionResult> TestMongo()
    {
        var users = await _context.Users.Find(_ => true).ToListAsync();
        return Ok(users.Count);
    }
}