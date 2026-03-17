using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace kdspro.Domain.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        /// <summary>
        /// Cambia el estado de la orden y gestiona automáticamente los timestamps.
        //  NUEVO: Se añaden parámetros opcionales para capturar los tiempos de eficiencia.
        /// </summary>
        Task UpdateStatusWithTimeAsync(string id, OrderStatus status, DateTime? startedAt = null, DateTime? readyAt = null, CancellationToken ct = default);

        /// <summary>
        /// Obtiene órdenes activas (Pending + Preparing) ordenadas FIFO.
        /// </summary>
        Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default);

        /// <summary>
        /// Obtiene órdenes listas para entrega (Ready).
        /// </summary>
        Task<List<Order>> GetReadyOrdersAsync(CancellationToken ct = default);

        /// <summary>
        /// Obtiene órdenes finalizadas (Delivered o Cancelled).
        /// </summary>
        Task<List<Order>> GetHistoryAsync(CancellationToken ct = default);

        // Mantenemos este por compatibilidad si es necesario, pero el de arriba es el que soluciona la eficiencia
        Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default);

        
    }
}
