using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using kdspro.Domain.Interfaces;
using kdspro.Domain.Entities;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository users, IConfiguration configuration)
    {
        _users = users;
        _configuration = configuration;
    }

    public async Task<(string? token, string? role)> Login(string username, string password)
    {
        // 1. Validación básica de entrada
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (null, null);

        // 2. Buscar usuario
        var user = await _users.GetByUsername(username);
        if (user == null)
            return (null, null);

        // 3. ✅ SOLO verificar el hash — BCrypt ya maneja la seguridad.
        //    Eliminamos la validación de "solo dígitos" que bloqueaba a kitchen/waiter.
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (null, null);

        // 4. Generar token
        var token = GenerateToken(user);
        return (token, user.Role);
    }

    private string GenerateToken(User user)
    {
        var keyString = _configuration["Jwt:Key"];

        if (string.IsNullOrEmpty(keyString))
            throw new Exception("JWT Key not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}