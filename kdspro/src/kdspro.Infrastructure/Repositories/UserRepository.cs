using MongoDB.Driver;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;

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
        return await _users
            .Find(u => u.Username == username)
            .FirstOrDefaultAsync();
    }

    public async Task Create(User user)
    {
        await _users.InsertOneAsync(user);
    }
}
