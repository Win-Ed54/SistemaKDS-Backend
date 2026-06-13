using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetById(string id);
    Task<User?> GetByUsername(string username);
    Task<User?> GetByEmail(string email);
    Task<List<User>> GetByRole(string role);
    Task<List<User>> GetAll();
    Task<bool> HasWaiterWithServiceScope(string serviceScope, string? excludeUserId = null);
    Task Create(User user);
    Task UpdatePasswordHash(string id, string passwordHash);
    Task UpdatePasswordState(string id, string passwordHash, bool mustChangePassword);
    Task UpdateCurrentSessionId(string id, string sessionId);
    Task UpdateServiceScope(string id, string serviceScope);
    Task UpdateLoginMetadata(string id, DateTime lastLoginAtUtc);
    Task UpdateActiveState(string id, bool isActive);
}
