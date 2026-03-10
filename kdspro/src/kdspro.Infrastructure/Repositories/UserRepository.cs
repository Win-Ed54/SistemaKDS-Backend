using MongoDB.Driver;
using kdspro.Domain.Entities;

namespace kdspro.Infrastructure.Repositories;

public class UserRepository
{
    private readonly IMongoCollection<User>_users;

    public UserRepository(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
    }

    public async Task<User> GetByUsername(string username)
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