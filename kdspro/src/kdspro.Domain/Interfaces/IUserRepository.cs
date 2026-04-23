using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetById(string id);
    Task<User?> GetByUsername(string username);
    Task<List<User>> GetByRole(string role);
    Task Create(User user);
    Task UpdatePasswordHash(string id, string passwordHash);
    Task UpdateCurrentSessionId(string id, string sessionId);
}
