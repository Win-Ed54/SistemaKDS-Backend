using MongoDB.Driver;
using MongoDB.Bson;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using System.Text.RegularExpressions;

namespace kdspro.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public UserRepository(MongoDbContext context)
    {
        _users = context.Users;
    }

    public async Task<User?> GetByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        var normalizedUsername = username.Trim();
        var usernameFilter = Builders<User>.Filter.Regex(
            user => user.Username,
            new BsonRegularExpression($"^\\s*{Regex.Escape(normalizedUsername)}\\s*$", "i"));

        return await _users
            .Find(usernameFilter)
            .FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var normalizedEmail = email.Trim();
        var emailFilter = Builders<User>.Filter.Regex(
            user => user.Email,
            new BsonRegularExpression($"^\\s*{Regex.Escape(normalizedEmail)}\\s*$", "i"));

        return await _users
            .Find(emailFilter)
            .FirstOrDefaultAsync();
    }

    public async Task<User?> GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        return await _users
            .Find(u => u.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetByRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return new List<User>();

        var normalizedRole = role.Trim();
        var roleFilter = Builders<User>.Filter.Regex(
            user => user.Role,
            new BsonRegularExpression($"^\\s*{Regex.Escape(normalizedRole)}\\s*$", "i"));

        return await _users
            .Find(roleFilter)
            .ToListAsync();
    }

    public async Task<List<User>> GetAll()
    {
        return await _users
            .Find(_ => true)
            .ToListAsync();
    }

    public async Task<bool> HasWaiterWithServiceScope(string serviceScope, string? excludeUserId = null)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(serviceScope)
            ? "hybrid"
            : serviceScope.Trim().ToLowerInvariant();

        var builder = Builders<User>.Filter;
        var filter = builder.Regex(user => user.Role, new BsonRegularExpression("^\\s*waiter\\s*$", "i")) &
                     builder.Regex(user => user.ServiceScope, new BsonRegularExpression($"^\\s*{Regex.Escape(normalizedScope)}\\s*$", "i"));

        if (!string.IsNullOrWhiteSpace(excludeUserId))
        {
            filter &= builder.Ne(user => user.Id, excludeUserId);
        }

        return await _users.Find(filter).AnyAsync();
    }

    public async Task Create(User user)
    {
        await _users.InsertOneAsync(user);
    }

    public async Task UpdatePasswordHash(string id, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(passwordHash)) return;

        await _users.UpdateOneAsync(
            user => user.Id == id,
            Builders<User>.Update.Set(user => user.PasswordHash, passwordHash));
    }

    public async Task UpdatePasswordState(string id, string passwordHash, bool mustChangePassword)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(passwordHash)) return;

        await _users.UpdateOneAsync(
            user => user.Id == id,
            Builders<User>.Update
                .Set(user => user.PasswordHash, passwordHash)
                .Set(user => user.MustChangePassword, mustChangePassword));
    }

    public async Task UpdateCurrentSessionId(string id, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        await _users.UpdateOneAsync(
            user => user.Id == id,
            Builders<User>.Update.Set(user => user.CurrentSessionId, sessionId ?? string.Empty));
    }

    public async Task UpdateLoginMetadata(string id, DateTime lastLoginAtUtc)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        await _users.UpdateOneAsync(
            user => user.Id == id,
            Builders<User>.Update.Set(user => user.LastLoginAt, lastLoginAtUtc));
    }

    public async Task UpdateServiceScope(string id, string serviceScope)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        await _users.UpdateOneAsync(
            user => user.Id == id,
            Builders<User>.Update.Set(user => user.ServiceScope, serviceScope ?? "hybrid"));
    }

    public async Task UpdateActiveState(string id, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        await _users.UpdateOneAsync(
            user => user.Id == id,
            Builders<User>.Update.Set(user => user.IsActive, isActive));
    }
}
