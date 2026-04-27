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
    Task<List<OrderDto>> GetHistory();
    Task<WaiterSummaryDto> GetWaiterSummary(string userId, string username);
    Task<IEnumerable<OrderDto>> GetWaiterOrdersToday(string waiterId);
    
    // Estados
    Task SetPreparing(string orderId, string preparedByName);
    Task SetReady(string orderId, string preparedByName);
    Task SetFinished(string orderId);
    Task MarkAsPaid(string orderId, string paidByName, MarkOrderPaidDto dto);
    Task CancelOrder(string orderId, string cancelledByName);
    
    // Control de Mesas
    Task CloseTable(int tableNumber, string? requesterUserId = null, bool isAdmin = false);
}
