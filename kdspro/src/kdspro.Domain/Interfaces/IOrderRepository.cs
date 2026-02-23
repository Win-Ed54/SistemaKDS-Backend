using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace kdspro.Domain.Interfaces
{
    /// <summary>
    /// Interfaz especializada para el repositorio de Órdenes (Módulo KDS - Mes 2).
    /// Hereda operaciones CRUD de IGenericRepository e incluye lógica de negocio para el flujo de cocina.
    /// </summary>
    public interface IOrderRepository : IGenericRepository<Order>
    {
        /// <summary>
        /// Actualiza el estado de una orden (Ej: de 'Pending' a 'Ready').
        /// Esta operación debe ser atómica y gestionar automáticamente la fecha de finalización (FinishedAt).
        /// </summary>
        /// <param name="id">Identificador único de la orden en MongoDB.</param>
        /// <param name="status">Nuevo estado del pedido (Enum).</param>
        /// <param name="ct">Token de cancelación opcional.</param>
        Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default);

        /// <summary>
        /// Recupera la lista de órdenes que aún no han sido entregadas o canceladas.
        /// Implementa la lógica de ordenamiento FIFO (First-In, First-Out) requerida para la pantalla de cocina.
        /// </summary>
        /// <param name="ct">Token de cancelación opcional.</param>
        /// <returns>Una lista de órdenes activas ordenadas por fecha de creación.</returns>
        Task<List<Order>> GetActiveOrdersAsync(CancellationToken ct = default);
    }
}