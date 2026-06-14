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

    public async Task<(string? token, string? role, string? userId, string? serviceScope, bool requiresPasswordChange)> Login(
        string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (null, null, null, null, false);

        var normalizedUsername = username.Trim();
        var user = await _users.GetByUsername(normalizedUsername);
        if (user == null || !user.IsActive) return (null, null, null, null, false);

        var isValidPassword = await VerifyPassword(user, password, AllowDemoLogin());
        if (!isValidPassword)
            return (null, null, null, null, false);

        var sessionId = Guid.NewGuid().ToString("N");
        await _users.UpdateCurrentSessionId(user.Id, sessionId);
        await _users.UpdateLoginMetadata(user.Id, DateTime.UtcNow);
        await RevokeAllRefreshTokens(user.Id);
        user.CurrentSessionId = sessionId;

        var role = NormalizeRole(user.Role);
        var serviceScope = NormalizeServiceScope(user.ServiceScope);

        return (GenerateToken(user), role, user.Id, serviceScope, user.MustChangePassword);
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
            new Claim(ClaimTypes.Role,           NormalizeRole(user.Role)),
            new Claim("service_scope",           NormalizeServiceScope(user.ServiceScope)),
            new Claim("sid",                     user.CurrentSessionId ?? string.Empty),
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
        var user = await _users.GetById(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.CurrentSessionId))
            throw new InvalidOperationException("User session not initialized.");

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var token = new RefreshToken
        {
            UserId = userId,
            SessionId = user.CurrentSessionId,
            Token = HashRefreshToken(rawToken),
            Expires = DateTime.UtcNow.AddDays(7),
        };
        await _refreshTokens.InsertOneAsync(token);
        return rawToken;
    }

    public async Task<RefreshResult?> RefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var tokenHash = HashRefreshToken(refreshToken);
        var stored = await _refreshTokens
            .Find(t => t.Token == tokenHash || t.Token == refreshToken)
            .FirstOrDefaultAsync();

        if (stored == null || !stored.IsActive) return null;

        // Buscar usuario por su Id guardado en el refresh token
        var user = await _users.GetById(stored.UserId);
        if (user == null) return null;
        if (string.IsNullOrWhiteSpace(user.CurrentSessionId)) return null;
        if (!string.Equals(stored.SessionId, user.CurrentSessionId, StringComparison.Ordinal))
            return null;

        // Revocar token viejo
        await _refreshTokens.UpdateOneAsync(
            Builders<RefreshToken>.Filter.Eq(t => t.Id, stored.Id),
            Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true)
        );

        var newJwt          = GenerateToken(user);
        var newRefreshToken = await GenerateRefreshToken(user.Id!);

        return new RefreshResult(newJwt, NormalizeRole(user.Role), NormalizeServiceScope(user.ServiceScope), newRefreshToken);
    }

    public async Task RevokeRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var tokenHash = HashRefreshToken(refreshToken);
        await _refreshTokens.UpdateOneAsync(
            Builders<RefreshToken>.Filter.Or(
                Builders<RefreshToken>.Filter.Eq(t => t.Token, tokenHash),
                Builders<RefreshToken>.Filter.Eq(t => t.Token, refreshToken)),
            Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true)
        );
    }

    public async Task RevokeAllRefreshTokens(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        await _refreshTokens.UpdateManyAsync(
            Builders<RefreshToken>.Filter.Eq(token => token.UserId, userId),
            Builders<RefreshToken>.Update.Set(token => token.IsRevoked, true));
    }

    public async Task Logout(string userId, string? refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await RevokeRefreshToken(refreshToken);
        }

        if (string.IsNullOrWhiteSpace(userId)) return;

        await RevokeAllRefreshTokens(userId);
        await _users.UpdateCurrentSessionId(userId, string.Empty);
    }

    public async Task<bool> RecoverPassword(string username, string newPassword, string recoveryKey)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(recoveryKey))
            return false;

        var configuredRecoveryKey = _configuration["Auth:RecoveryKey"];
        if (!IsValidRecoveryKey(configuredRecoveryKey, recoveryKey))
            return false;

        var user = await _users.GetByUsername(username.Trim());
        if (user == null) return false;

        await _users.UpdatePasswordState(user.Id, BCrypt.Net.BCrypt.HashPassword(newPassword), false);
        return true;
    }

    public async Task<bool> ChangePassword(string userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword))
            return false;

        var user = await _users.GetById(userId);
        if (user == null || !user.IsActive) return false;

        var currentPasswordMatches = await VerifyPassword(user, currentPassword, AllowDemoLogin());
        if (!currentPasswordMatches) return false;

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            return false;

        await _users.UpdatePasswordState(user.Id, BCrypt.Net.BCrypt.HashPassword(newPassword), false);
        return true;
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    private static bool IsValidRecoveryKey(string? configuredRecoveryKey, string providedRecoveryKey)
    {
        if (string.IsNullOrWhiteSpace(configuredRecoveryKey) ||
            configuredRecoveryKey.Length < 32 ||
            string.IsNullOrWhiteSpace(providedRecoveryKey))
            return false;

        var configuredBytes = Encoding.UTF8.GetBytes(configuredRecoveryKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedRecoveryKey);

        return configuredBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes);
    }

    private bool AllowDemoLogin()
    {
        return bool.TryParse(_configuration["Auth:AllowDemoLogin"], out var enabled) && enabled;
    }

    private async Task<bool> VerifyPassword(User user, string password, bool allowDemoLogin)
    {
        if (allowDemoLogin &&
            user.IsDemoAccount &&
            string.IsNullOrWhiteSpace(password))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash)) return false;

        if (IsBCryptHash(user.PasswordHash))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        if (!string.Equals(user.PasswordHash, password, StringComparison.Ordinal))
            return false;

        await _users.UpdatePasswordHash(user.Id, BCrypt.Net.BCrypt.HashPassword(password));
        return true;
    }

    private static bool IsBCryptHash(string passwordHash)
    {
        return passwordHash.StartsWith("$2a$", StringComparison.Ordinal) ||
               passwordHash.StartsWith("$2b$", StringComparison.Ordinal) ||
               passwordHash.StartsWith("$2x$", StringComparison.Ordinal) ||
               passwordHash.StartsWith("$2y$", StringComparison.Ordinal);
    }

    private static string NormalizeRole(string role)
    {
        return string.IsNullOrWhiteSpace(role)
            ? string.Empty
            : role.Trim().ToLowerInvariant();
    }

    private static string NormalizeServiceScope(string serviceScope)
    {
        var normalized = string.IsNullOrWhiteSpace(serviceScope)
            ? "hybrid"
            : serviceScope.Trim().ToLowerInvariant();

        return normalized is "dining" or "takeout" or "hybrid"
            ? normalized
            : "hybrid";
    }
}

public record RefreshResult(string Token, string Role, string ServiceScope, string NewRefreshToken);
