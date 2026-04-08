using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetById(string id);
    Task<User?> GetByUsername(string username);
    Task Create(User user);
}
