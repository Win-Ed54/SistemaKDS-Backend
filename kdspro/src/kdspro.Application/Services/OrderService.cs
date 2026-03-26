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

    public async Task<OrderDto> CreateOrder(CreateOrderDto dto, string userId, string username)
{
    // 1. VALIDACIÓN PREVENTIVA: Comprobar stock de TODO antes de descontar nada
    foreach (var item in dto.Items)
    {
        var product = await _productRepository.GetByIdAsync(item.ProductId);
        if (product == null || product.Stock < item.Quantity)
        {
            // Avisar al mesero específicamente qué producto falló
            await _notificationService.NotifyProductOutOfStock(item.ProductId);
            throw new Exception($"Stock insuficiente para: {item.ProductName}. Disponible: {product?.Stock ?? 0}");
        }
    }

    var updatedStocks = new Dictionary<string, int>();

    // 2. PROCESAMIENTO: Ahora que sabemos que hay stock, descontamos y notificamos
    foreach (var item in dto.Items)
    {
        await _productRepository.DeductStockAsync(item.ProductId, item.Quantity);
        var updated = await _productRepository.GetByIdAsync(item.ProductId);
        
        if (updated != null)
        {
            updatedStocks[item.ProductId] = updated.Stock;
            // 📢 Notifica a TODOS los meseros (Edwin, Rene, etc.) el nuevo stock real al instante
            await _notificationService.NotifyStockUpdated(updated.Id!, updated.Stock);
        }
    }

    // 3. PERSISTENCIA: Crear la entidad de Orden
    var order = new Order
    {
        TableNumber  = dto.TableNumber,
        CustomerName = dto.CustomerName,
        WaiterId     = userId,
        WaiterName   = username,
        Status       = OrderStatus.Pending,
        CreatedAt    = DateTime.UtcNow,
        Items = dto.Items.Select(i => new OrderItem
        {
            ProductId   = i.ProductId,
            ProductName = i.ProductName,
            Quantity    = i.Quantity,
            Notes       = i.Notes ?? ""
        }).ToList()
    };

    await _orderRepository.CreateAsync(order);

    // 4. ESTADO DE MESA Y COCINA
    await _tableRepository.SetOccupiedAsync(dto.TableNumber, true);
    await _notificationService.NotifyNewOrder(order);

    // 5. RESPUESTA: Mapeo con stocks actuales para el frontend
    var resultDto = MapToDto(order);
    foreach (var itemDto in resultDto.Items)
        if (updatedStocks.TryGetValue(itemDto.ProductId, out int s))
            itemDto.CurrentStock = s;

    return resultDto;
}


    // --- FLUJO DE ESTADOS ---

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
        // La cocina marca como entregado, pero LA MESA SIGUE OCUPADA
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Delivered);
        await _notificationService.NotifyOrderDelivered(orderId);
    }

    // NUEVO: Solo Admin/Caja libera la mesa al cobrar
    public async Task CloseTable(int tableNumber)
    {
        await _tableRepository.SetOccupiedAsync(tableNumber, false);
        await _notificationService.NotifyTableStatusUpdated(tableNumber, false);
    }

    // --- QUERIES Y REPORTES ---

    public async Task<IEnumerable<OrderDto>> GetWaiterOrdersToday(string waiterName)
    {
        var allOrders = await _orderRepository.GetOrdersByWaiterAsync(waiterName);
        var today = DateTime.UtcNow.Date;

        return allOrders
            .Where(o => o.CreatedAt >= today)
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapToDto)
            .ToList();
    }

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

    public async Task CancelOrder(string orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) return;

        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);

        foreach (var item in order.Items)
        {
            await _productRepository.RestoreStockAsync(item.ProductId, item.Quantity);
            var updated = await _productRepository.GetByIdAsync(item.ProductId);
            if (updated != null)
                await _notificationService.NotifyStockUpdated(item.ProductId, updated.Stock);
        }

        var stillActive = await _orderRepository.HasActiveOrdersForTableAsync(order.TableNumber, orderId);
        if (!stillActive)
            await _tableRepository.SetOccupiedAsync(order.TableNumber, false);

        await _notificationService.NotifyOrderCancelled(orderId);
    }

    public async Task<OrderDto?> GetOrderById(string id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<List<OrderDto>> GetActiveOrders() => 
        (await _orderRepository.GetActiveOrdersAsync()).Select(MapToDto).ToList();

    public async Task<List<OrderDto>> GetHistory() => 
        (await _orderRepository.GetHistoryAsync()).Select(MapToDto).ToList();

    public async Task<List<OrderDto>> GetMyOrders(string userId) =>
        (await _orderRepository.GetOrdersByWaiterAsync(userId))
            .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
            .Select(MapToDto)
            .ToList();

    public async Task<List<OrderDto>> GetReadyOrders() =>
        (await _orderRepository.GetActiveOrdersAsync())
            .Where(o => o.Status == OrderStatus.Ready)
            .Select(MapToDto)
            .ToList();

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
}
