/// <summary>
/// Interfaz genérica para el patrón Repositorio (Capa Domain).
/// Define las operaciones CRUD estándar para cualquier entidad del sistema KDS.
/// </summary>
/// <typeparam name="T">La entidad de dominio (Product, Table, Order, etc.).</typeparam>
public interface IGenericRepository<T> where T : class
{
    /// <summary>
    /// Recupera todos los registros de la colección de forma asincrónica.
    /// </summary>
    /// <param name="ct">Token para cancelar la operación si el cliente cierra la conexión.</param>
    /// <returns>Una colección enumerable de la entidad T.</returns>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Busca un registro específico por su identificador único (ObjectId de MongoDB).
    /// </summary>
    /// <param name="id">ID del documento en formato string.</param>
    /// <param name="ct">Token de cancelación opcional.</param>
    /// <returns>La entidad encontrada o null si no existe.</returns>
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Inserta un nuevo documento de forma atómica en la base de datos.
    /// </summary>
    /// Objeto de la entidad a persistir.</param>
    /// <param name="ct">Token de cancelación opcional.</param>
    Task CreateAsync(T entity, CancellationToken ct = default);

    /// <summary>
    /// Reemplaza un documento existente con una nueva versión de la entidad.
    /// </summary>
    /// <param name="id">ID del documento a actualizar.</param>
    /// Objeto con los nuevos datos.</param>
    /// <param name="ct">Token de cancelación opcional.</param>
    Task UpdateAsync(string id, T entity, CancellationToken ct = default);
}
