using kdspro.Application.DTOs;
using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IOrderNotificationService
{
    // ----------------------------
    // ORDENES
    // ----------------------------

    Task NotifyNewOrder(OrderDto order);
    Task NotifyPendingPrepaymentOrder(OrderDto order);

    Task NotifyOrderPreparing(OrderDto order);

    Task NotifyOrderReady(OrderDto order);

    Task NotifyOrderDelivered(OrderDto order);

    Task NotifyOrderPaid(OrderDto order);

    Task NotifyOrderCancelled(OrderDto order);

    // ----------------------------
    // STOCK
    // ----------------------------

    Task NotifyStockUpdated(string productId, int newStock);

    Task NotifyProductOutOfStock(string productId);

    // ----------------------------
    // MESAS
    // ----------------------------

    Task NotifyTableStatusUpdated(Table table);
}
