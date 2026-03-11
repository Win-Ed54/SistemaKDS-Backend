using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsername(string username);
    Task Create(User user);
}
