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

    public async Task UpdateCurrentSessionId(string id, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        await _users.UpdateOneAsync(
            user => user.Id == id,
            Builders<User>.Update.Set(user => user.CurrentSessionId, sessionId ?? string.Empty));
    }
}
