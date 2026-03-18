using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using kdspro.Domain.Interfaces;
using MongoDB.Driver;

namespace kdspro.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ITableRepository _tableRepository;         // ✅ NUEVO
    private readonly IOrderNotificationService _notificationService;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ITableRepository tableRepository,                       // ✅ NUEVO
        IOrderNotificationService notificationService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _tableRepository = tableRepository;                     // ✅ NUEVO
        _notificationService = notificationService;
    }

    public async Task<OrderDto> CreateOrder(CreateOrderDto dto)
    {
        var updatedStocks = new Dictionary<string, int>();

        // 1. VALIDACIÓN Y DESCUENTO DE STOCK
        foreach (var item in dto.Items)
        {
            bool success = await _productRepository.DeductStockAsync(item.ProductId, item.Quantity);
            if (!success) throw new Exception($"Stock insuficiente para: {item.ProductName}");

            var updatedProduct = await _productRepository.GetByIdAsync(item.ProductId);
            if (updatedProduct != null)
            {
                updatedStocks[item.ProductId] = updatedProduct.Stock;
                if (updatedProduct.Stock <= 0)
                    await _notificationService.NotifyProductOutOfStock(item.ProductId);
            }
        }

        // 2. CREACIÓN DE LA ORDEN
        var order = new Order
        {
            TableNumber = dto.TableNumber,
            CustomerName = dto.CustomerName,
            WaiterName = dto.WaiterName,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StartedAt = null,
            ReadyAt = null,
            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Notes = i.Notes ?? ""
            }).ToList()
        };

        await _orderRepository.CreateAsync(order);

        // ✅ 3. MARCAR MESA COMO OCUPADA
        await _tableRepository.SetOccupiedAsync(dto.TableNumber, true);

        // 4. NOTIFICAR A COCINA Y ADMIN
        await _notificationService.NotifyNewOrder(order);

        var resultDto = MapToDto(order);
        foreach (var itemDto in resultDto.Items)
        {
            if (updatedStocks.TryGetValue(itemDto.ProductId, out int currentStock))
                itemDto.CurrentStock = currentStock;
        }

        return resultDto;
    }

    public async Task SetPreparing(string orderId)
    {
        var startTime = DateTime.UtcNow;
        await _orderRepository.UpdateStatusWithTimeAsync(orderId, OrderStatus.Preparing, startTime, null);

        var order = await GetOrderById(orderId);
        if (order != null) await _notificationService.NotifyOrderPreparing(order);
    }

    public async Task SetReady(string orderId)
    {
        var readyTime = DateTime.UtcNow;
        await _orderRepository.UpdateStatusWithTimeAsync(orderId, OrderStatus.Ready, null, readyTime);

        var order = await GetOrderById(orderId);
        if (order != null) await _notificationService.NotifyOrderReady(order);
    }

    public async Task SetFinished(string orderId)
    {
        // Obtener la orden ANTES de cambiar estado (necesitamos el TableNumber)
        var order = await _orderRepository.GetByIdAsync(orderId);

        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Delivered);

        // ✅ LIBERAR MESA si no quedan más órdenes activas en ella
        if (order != null)
        {
            var stillActive = await _orderRepository.HasActiveOrdersForTableAsync(order.TableNumber, orderId);
            if (!stillActive)
                await _tableRepository.SetOccupiedAsync(order.TableNumber, false);
        }

        await _notificationService.NotifyOrderDelivered(orderId);
    }

    public async Task CancelOrder(string orderId)
    {
        // Misma lógica que SetFinished: liberar mesa si no hay más órdenes
        var order = await _orderRepository.GetByIdAsync(orderId);

        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);

        // ✅ LIBERAR MESA si no quedan más órdenes activas
        if (order != null)
        {
            var stillActive = await _orderRepository.HasActiveOrdersForTableAsync(order.TableNumber, orderId);
            if (!stillActive)
                await _tableRepository.SetOccupiedAsync(order.TableNumber, false);
        }

        await _notificationService.NotifyOrderCancelled(orderId);
    }

    // --- GETTERS ---
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

    // --- MAPPER ---
    private static OrderDto MapToDto(Order order) => new()
    {
        Id         = order.Id!,
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
}