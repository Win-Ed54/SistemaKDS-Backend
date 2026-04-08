using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using kdspro.Domain.Interfaces;
using kdspro.Domain.Entities;

namespace kdspro.Api.Services;

public class AuthService
{
    private readonly IUserRepository  _users;
    private readonly IConfiguration   _configuration;
    private readonly IMongoCollection<RefreshToken> _refreshTokens;

    public AuthService(
        IUserRepository users,
        IConfiguration  configuration,
        IMongoDatabase  database)
    {
        _users         = users;
        _configuration = configuration;
        _refreshTokens = database.GetCollection<RefreshToken>("RefreshTokens");
    }

    public async Task<(string? token, string? role, string? userId)> Login(
        string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (null, null, null);

        var user = await _users.GetByUsername(username);
        if (user == null) return (null, null, null);

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (null, null, null);

        return (GenerateToken(user), user.Role, user.Id);
    }

    private string GenerateToken(User user)
    {
        var keyString = _configuration["Jwt:Key"]
            ?? throw new Exception("JWT Key not configured");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Role,           user.Role),
        };

        var token = new JwtSecurityToken(
            issuer:             _configuration["Jwt:Issuer"],
            audience:           _configuration["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken(string userId)
    {
        var token = new RefreshToken
        {
            UserId  = userId,
            Token   = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        await _refreshTokens.InsertOneAsync(token);
        return token.Token;
    }

    // ✅ UN SOLO método RefreshToken
    public async Task<RefreshResult?> RefreshToken(string refreshToken)
    {
        var stored = await _refreshTokens
            .Find(t => t.Token == refreshToken)
            .FirstOrDefaultAsync();

        if (stored == null || !stored.IsActive) return null;

        // Buscar usuario por su Id guardado en el refresh token
        var user = await _users.GetById(stored.UserId);
        if (user == null) return null;

        // Revocar token viejo
        await _refreshTokens.UpdateOneAsync(
            Builders<RefreshToken>.Filter.Eq(t => t.Id, stored.Id),
            Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true)
        );

        var newJwt          = GenerateToken(user);
        var newRefreshToken = await GenerateRefreshToken(user.Id!);

        return new RefreshResult(newJwt, user.Role, newRefreshToken);
    }

    public async Task RevokeRefreshToken(string refreshToken)
    {
        await _refreshTokens.UpdateOneAsync(
            Builders<RefreshToken>.Filter.Eq(t => t.Token,      refreshToken),
            Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true)
        );
    }
}

public record RefreshResult(string Token, string Role, string NewRefreshToken);
