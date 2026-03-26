using kdspro.Domain.Entities;
using kdspro.Application.DTOs;
 // Asegúrate de importar tus DTOs

namespace kdspro.Domain.Interfaces;

public interface IOrderNotificationService
{
    // --- MÉTODOS EXISTENTES ---
    Task NotifyNewOrder(Order order);
    Task NotifyProductOutOfStock(string productId);
    
    // --- NUEVOS MÉTODOS PARA EL ADMIN Y SINCRONIZACIÓN ---
    // Notifica que la orden entró a cocina (Para el color en Admin)
    Task NotifyOrderPreparing(OrderDto order);
    
    // Notifica que la orden está lista (Para el aviso al mesero y eficiencia en Admin)
    Task NotifyOrderReady(OrderDto order);
    
    // Notifica que la orden se entregó (Para liberar la mesa en el Dashboard)
    Task NotifyOrderDelivered(string orderId);
    
    // Notifica cancelación (Para limpiar el resumen del Admin)
    Task NotifyOrderCancelled(string orderId);

    Task NotifyStockUpdated(string productId, int newStock);

    // Añadir al final de la interfaz
    Task NotifyTableStatusUpdated(int tableNumber, bool isOccupied);

}
