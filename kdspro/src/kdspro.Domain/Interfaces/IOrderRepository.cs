using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace kdspro.Domain.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        /// <summary>
        /// Cambia el estado de la orden y gestiona automáticamente los timestamps.
        /// </summary>
        Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default);

        /// <summary>
        /// Obtiene órdenes activas (Pending + Preparing) ordenadas FIFO.
        /// </summary>
        Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default);

        /// 🔥 NUEVO (opcional)
        /// <summary>
        /// Obtiene órdenes listas para entrega (Ready).
        /// </summary>
        Task<List<Order>> GetReadyOrdersAsync(CancellationToken ct = default);

        /// 🔥 NUEVO (opcional)
        /// <summary>
        /// Obtiene órdenes finalizadas (Delivered o Cancelled).
        /// </summary>
        Task<List<Order>> GetHistoryAsync(CancellationToken ct = default);
    }
}
