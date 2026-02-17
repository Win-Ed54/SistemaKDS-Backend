using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using System.Threading;
namespace kdspro.Domain.Interfaces;

// Al heredar de IGenericRepository<Order>, ya tienes GetAllAsync, GetByIdAsync y CreateAsync
public interface IOrderRepository : IGenericRepository<Order>
{
    // AQUÍ SOLO VA LO QUE NO ES CRUD GENÉRICO
    Task UpdateStatusAsync(string id, OrderStatus status, CancellationToken ct = default);
}