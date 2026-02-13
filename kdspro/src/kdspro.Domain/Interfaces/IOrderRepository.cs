using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using kdspro.Domain.Interfaces;


namespace kdspro.Domain.Interfaces;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();
    Task CreateAsync(Order order);
    Task UpdateStatusAsync(string id, OrderStatus status); // Para marcar como "Cocinando" o "Listo"
}