using System.Collections.Generic;
using System.Threading.Tasks;
using kdspro.Application.DTOs;

namespace kdspro.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrder(CreateOrderDto dto, string userId, string username);
    Task<OrderDto?> GetOrderById(string id);
    Task<List<OrderDto>> GetMyOrders(string userId);
    Task<List<OrderDto>> GetActiveOrders();
    Task<List<OrderDto>> GetReadyOrders();
    Task<List<OrderDto>> GetHistory();
    Task<WaiterSummaryDto> GetWaiterSummary(string userId, string username);
    Task<IEnumerable<OrderDto>> GetWaiterOrdersToday(string waiterName); // Solo una vez
    
    // Estados
    Task SetPreparing(string orderId);
    Task SetReady(string orderId);
    Task SetFinished(string orderId);
    Task MarkAsPaid(string orderId);
    Task CancelOrder(string orderId);
    
    // Control de Mesas
    Task CloseTable(int tableNumber);
}
