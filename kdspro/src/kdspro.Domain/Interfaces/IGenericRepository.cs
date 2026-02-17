public interface IGenericRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task CreateAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(string id, T entity, CancellationToken ct = default);

    

}

