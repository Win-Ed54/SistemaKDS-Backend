using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetById(string id);
    Task<User?> GetByUsername(string username);
    Task<List<User>> GetByRole(string role);
    Task<List<User>> GetAll();
    Task<bool> HasWaiterWithServiceScope(string serviceScope, string? excludeUserId = null);
    Task Create(User user);
    Task UpdatePasswordHash(string id, string passwordHash);
    Task UpdateCurrentSessionId(string id, string sessionId);
    Task UpdateServiceScope(string id, string serviceScope);
}
