using Microsoft.AspNetCore.Mvc;
using kdspro.Application.DTOs;
using kdspro.Infrastructure.Repositories;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _users;

    public AuthController(UserRepository users)
    {
        _users = users;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _users.GetByUsername(dto.Username);

        if(user == null)
           return Unauthorized("Usuario no exite");
        if(user.PasswordHash != dto.Password)
           return Unauthorized("Password incorrecto");

        return Ok(new
        {
          username = user.Username,
          role = user.Role  
        });     
    }
}