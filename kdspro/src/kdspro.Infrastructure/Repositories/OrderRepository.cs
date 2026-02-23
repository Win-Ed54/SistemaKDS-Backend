using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;
using kdspro.Domain.Enums;

namespace kdspro.Infrastructure.Repositories;

/// <summary>
/// Implementación especializada del repositorio de Órdenes (Capa Infrastructure).
/// Gestiona la persistencia en MongoDB con optimizaciones de rendimiento para el flujo KDS.
/// </summary>
public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    /// <summary>
    /// Constructor que inicializa la colección "Orders" y configura el rendimiento de la base de datos.
    /// </summary>
    public OrderRepository(MongoDbContext context) : base(context, "Orders")
    {
        // --- OPTIMIZACIÓN DE RENDIMIENTO (MES 2) ---
        // Se crea un índice compuesto: Filtra por 'Status' y ordena por 'CreatedAt'.
        // REQUISITO: Garantiza que la consulta FIFO sea instantánea incluso bajo estrés.
        var indexKeys = Builders<Order>.IndexKeys
            .Ascending(o => o.Status)
            .Ascending(o => o.CreatedAt);

        var indexModel = new CreateIndexModel<Order>(indexKeys, new CreateIndexOptions { Name = "idx_orders_active_fifo" });
        
        // Ejecución única al arrancar el repositorio.
        _collection.Indexes.CreateOne(indexModel);
    }

    /// <summary>
    /// Actualiza el estado de la orden y gestiona automáticamente la auditoría de tiempo.
    /// Registra 'FinishedAt' solo cuando el pedido sale de la cocina (Ready/Delivered).
    /// </summary>
    /// <param name="id">ID de la orden en MongoDB.</param>
    /// <param name="status">Nuevo estado (Enum).</param>
    public async Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.Id, id);
    
        // 1. LÓGICA DE TIEMPOS: Calculamos la fecha de finalización solo si el estado es final.
        // Si la orden vuelve a 'Cooking' por error, el campo se limpia (null).
        DateTime? finishedDate = (status == OrderStatus.Ready || status == OrderStatus.Delivered) 
                                ? DateTime.UtcNow 
                                : null;

        // 2. ACTUALIZACIÓN ATÓMICA: Se envía un solo paquete de datos a MongoDB.
        // Mejora el rendimiento y evita inconsistencias en la base de datos.
        var update = Builders<Order>.Update
           .Set(o => o.Status, status)
           .Set(o => o.FinishedAt, finishedDate);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>
    /// Recupera las órdenes vigentes para la pantalla de cocina (KDS Web View).
    /// Aplica lógica estricta de ordenamiento por hora de llegada (FIFO).
    /// </summary>
    public async Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default)
    {
        // 1. FILTRADO: Excluimos órdenes entregadas o canceladas que ya no deben estar en cocina.
        var filter = Builders<Order>.Filter.And(
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Delivered),
            Builders<Order>.Filter.Ne(o => o.Status, OrderStatus.Cancelled)
        );

        // 2. ORDENAMIENTO: Las órdenes más antiguas (menor CreatedAt) aparecen primero.
        var sort = Builders<Order>.Sort.Ascending(o => o.CreatedAt);

        // 3. CONSULTA INDEXADA: MongoDB utiliza el índice compuesto creado en el constructor.
        return await _collection.Find(filter)
                                 .Sort(sort)
                                 .ToListAsync(ct);
    }
}