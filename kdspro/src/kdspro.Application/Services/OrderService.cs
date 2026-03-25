using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using kdspro.Domain.Interfaces;

namespace kdspro.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository    _orderRepository;
    private readonly IProductRepository  _productRepository;
    private readonly ITableRepository    _tableRepository;
    private readonly IOrderNotificationService _notificationService;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ITableRepository tableRepository,
        IOrderNotificationService notificationService)
    {
        _orderRepository     = orderRepository;
        _productRepository   = productRepository;
        _tableRepository     = tableRepository;
        _notificationService = notificationService;
    }

    public async Task<OrderDto> CreateOrder(CreateOrderDto dto, string userId, string username )
    {
        var updatedStocks = new Dictionary<string, int>();

        foreach (var item in dto.Items)
        {
            bool success = await _productRepository.DeductStockAsync(item.ProductId, item.Quantity);
            if (!success) throw new Exception($"Stock insuficiente para: {item.ProductName}");

            var updated = await _productRepository.GetByIdAsync(item.ProductId);
            if (updated != null)
            {
                updatedStocks[item.ProductId] = updated.Stock;
                if (updated.Stock <= 0)
                    await _notificationService.NotifyProductOutOfStock(item.ProductId);
            }
        }

        var order = new Order
        {
            TableNumber  = dto.TableNumber,
            CustomerName = dto.CustomerName,
            WaiterId     = userId,
            WaiterName   = username,
            Status       = OrderStatus.Pending,
            CreatedAt    = DateTime.UtcNow,
            StartedAt    = null,
            ReadyAt      = null,
            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId   = i.ProductId,
                ProductName = i.ProductName,
                Quantity    = i.Quantity,
                Notes       = i.Notes ?? ""
            }).ToList()
        };

        await _orderRepository.CreateAsync(order);
        foreach (var item in dto.Items)
{
    var product = await _productRepository.GetByIdAsync(item.ProductId);
    if (product != null)
    {
        // Esto envía el nuevo stock a todos los meseros y admins vía SignalR
        await _notificationService.NotifyStockUpdated(product.Id!, product.Stock);
    }
}

await _tableRepository.SetOccupiedAsync(dto.TableNumber, true);
await _notificationService.NotifyNewOrder(order); // Notifica a Cocina y Admin [cite: 55]

        await _tableRepository.SetOccupiedAsync(dto.TableNumber, true);
        await _notificationService.NotifyNewOrder(order);

        var resultDto = MapToDto(order);
        foreach (var itemDto in resultDto.Items)
            if (updatedStocks.TryGetValue(itemDto.ProductId, out int s))
                itemDto.CurrentStock = s;

        return resultDto;
    }

    public async Task SetPreparing(string orderId)
    {
        await _orderRepository.UpdateStatusWithTimeAsync(orderId, OrderStatus.Preparing, DateTime.UtcNow, null);
        var order = await GetOrderById(orderId);
        if (order != null) await _notificationService.NotifyOrderPreparing(order);
    }

    public async Task SetReady(string orderId)
    {
        await _orderRepository.UpdateStatusWithTimeAsync(orderId, OrderStatus.Ready, null, DateTime.UtcNow);
        var order = await GetOrderById(orderId);
        if (order != null) await _notificationService.NotifyOrderReady(order);
    }

    public async Task SetFinished(string orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Delivered);

        if (order != null)
        {
            var stillActive = await _orderRepository.HasActiveOrdersForTableAsync(order.TableNumber, orderId);
            if (!stillActive)
                await _tableRepository.SetOccupiedAsync(order.TableNumber, false);
        }

        await _notificationService.NotifyOrderDelivered(orderId);
    }

    public async Task<List<OrderDto>> GetMyOrders(string userId)
    {
        var orders = await _orderRepository.GetOrdersByWaiterAsync(userId);
        return orders.Select(MapToDto).ToList();
    }


    // CANCELAR CON DEVOLUCION DE STOCK
    public async Task CancelOrder(string orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) return;

        // 1. Cancelar en DB
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);

        // 2. Devolver stock de cada item
        foreach (var item in order.Items)
        {
            await _productRepository.RestoreStockAsync(item.ProductId, item.Quantity);

            var updated = await _productRepository.GetByIdAsync(item.ProductId);
            if (updated != null)
                await _notificationService.NotifyStockUpdated(item.ProductId, updated.Stock);
        }

        // 3. Liberar mesa si no quedan ordenes activas
        var stillActive = await _orderRepository.HasActiveOrdersForTableAsync(order.TableNumber, orderId);
        if (!stillActive)
            await _tableRepository.SetOccupiedAsync(order.TableNumber, false);

        // 4. Notificar cancelacion
        await _notificationService.NotifyOrderCancelled(orderId);
    }

    public async Task<OrderDto?> GetOrderById(string id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<List<OrderDto>> GetActiveOrders()
    {
        var orders = await _orderRepository.GetActiveOrdersAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetReadyOrders()
    {
        var orders = await _orderRepository.GetReadyOrdersAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetHistory()
    {
        var orders = await _orderRepository.GetHistoryAsync();
        return orders.Select(MapToDto).ToList();
    }

    private static OrderDto MapToDto(Order order) => new()
    {
        Id           = order.Id!,
        TableNumber  = order.TableNumber,
        CustomerName = order.CustomerName,
        WaiterName   = order.WaiterName,
        Status       = order.Status,
        CreatedAt    = order.CreatedAt,
        StartedAt    = order.StartedAt,
        ReadyAt      = order.ReadyAt,
        Items = order.Items?.Select(i => new OrderItemDto
        {
            ProductId    = i.ProductId,
            ProductName  = i.ProductName,
            Quantity     = i.Quantity,
            Notes        = i.Notes,
            CurrentStock = 0
        }).ToList() ?? []
    };

    public async Task<WaiterSummaryDto> GetWaiterSummary(string userId, string username)
    {
        var allOrders = await _orderRepository.GetOrdersByWaiterAsync(userId);

        return new WaiterSummaryDto
        {
            WaiterId = userId,
            WaiterName = username,
            TotalCreated = allOrders.Count,
            TotalDelivered = allOrders.Count(o => o.Status == OrderStatus.Delivered),
            MyActiveOrders = allOrders
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                .Select(MapToDto)
                .ToList()
        };
    }

    public async Task<IEnumerable<OrderDto>> GetWaiterOrdersToday(string waiterName)
    {
        // Obtenemos todas las órdenes del mesero usando el repositorio existente
        var allOrders = await _orderRepository.GetOrdersByWaiterAsync(waiterName);

        // Filtramos en memoria por la fecha de hoy para mayor simplicidad 
        // (o puedes añadir el método especializado al IOrderRepository)
        var today = DateTime.UtcNow.Date;

        return allOrders
            .Where(o => o.CreatedAt >= today)
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

}