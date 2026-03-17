using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IOrderNotificationService
{
    // Notificar nueva orden a la cocina
    Task NotifyNewOrder(Order order);
    
    // Notificar stock agotado a los meseros
    Task NotifyProductOutOfStock(string productId);
}
