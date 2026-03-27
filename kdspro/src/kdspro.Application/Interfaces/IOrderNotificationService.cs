using kdspro.Application.DTOs;

namespace kdspro.Domain.Interfaces;

public interface IOrderNotificationService
{
    // ----------------------------
    // ORDENES
    // ----------------------------

    Task NotifyNewOrder(OrderDto order);

    Task NotifyOrderPreparing(OrderDto order);

    Task NotifyOrderReady(OrderDto order);

    Task NotifyOrderDelivered(OrderDto order);

    Task NotifyOrderCancelled(OrderDto order);

    // ----------------------------
    // STOCK
    // ----------------------------

    Task NotifyStockUpdated(string productId, int newStock);

    Task NotifyProductOutOfStock(string productId);

    // ----------------------------
    // MESAS
    // ----------------------------

    Task NotifyTableStatusUpdated(int tableNumber, bool isOccupied);
}
