using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using kdspro.Domain.Interfaces;

namespace kdspro.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ITableRepository _tableRepository;
    private readonly IOrderNotificationService _notificationService;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ITableRepository tableRepository,
        IOrderNotificationService notificationService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _tableRepository = tableRepository;
        _notificationService = notificationService;
    }

    public async Task<OrderDto> CreateOrder(CreateOrderDto dto, string userId, string username)
    {
        // 1. VALIDACIÓN PREVENTIVA
        foreach (var item in dto.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null || product.Stock < item.Quantity)
            {
                await _notificationService.NotifyProductOutOfStock(item.ProductId);
                throw new Exception($"Stock insuficiente para: {item.ProductName}. Disponible: {product?.Stock ?? 0}");
            }
        }

        var updatedStocks = new Dictionary<string, int>();

        // 2. DESCONTAR STOCK
        // foreach (var item in dto.Items)
        //{
        //  await _productRepository.DeductStockAsync(item.ProductId, item.Quantity);
        //var updated = await _productRepository.GetByIdAsync(item.ProductId);

        //if (updated != null)
        //{
        //  updatedStocks[item.ProductId] = updated.Stock;
        //await _notificationService.NotifyStockUpdated(updated.Id!, updated.Stock);
        //}
        //}
        // 2. DESCONTAR STOCK (Implementación de Concurrencia Atómica)
        foreach (var item in dto.Items)
        {
            // Intentamos descontar directamente. El Repo devolverá false si el stock bajó 
            // de la cantidad solicitada mientras el mesero procesaba la orden.
            bool success = await _productRepository.DeductStockAsync(item.ProductId, item.Quantity);

            if (!success)
            {
                // Si falla, notificamos a todos que este producto ya no tiene stock suficiente
                await _notificationService.NotifyProductOutOfStock(item.ProductId);

                throw new Exception($"¡Lo sentimos! Ya no hay stock suficiente para: {item.ProductName}.");
            }

            // Si tuvo éxito, obtenemos el valor real actualizado para sincronizar a los demás meseros
            var updated = await _productRepository.GetByIdAsync(item.ProductId);
            if (updated != null)
            {
                updatedStocks[item.ProductId] = updated.Stock;
                await _notificationService.NotifyStockUpdated(updated.Id!, updated.Stock);
            }
        }


        // 3. CREAR ORDEN
        var order = new Order
        {
            TableNumber = dto.TableNumber,
            CustomerName = dto.CustomerName,
            WaiterId = userId,
            WaiterName = username,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.Price,
                Quantity = i.Quantity,
                Notes = i.Notes ?? ""
            }).ToList()
        };

        await _orderRepository.CreateAsync(order);

        // 4. ACTUALIZAR MESA + NOTIFICAR (ORDEN IMPORTANTE)
        await _tableRepository.SetOccupiedAsync(dto.TableNumber, true);
        await _notificationService.NotifyTableStatusUpdated(dto.TableNumber, true);

        // 5. NOTIFICAR ORDEN
        
        await _notificationService.NotifyNewOrder(MapToDto(order));

        // 6. RESPUESTA
        var resultDto = MapToDto(order);

        foreach (var itemDto in resultDto.Items)
        {
            if (updatedStocks.TryGetValue(itemDto.ProductId, out int stock))
                itemDto.CurrentStock = stock;
        }

        return resultDto;
    }

    // ----------------------------
    // ESTADOS DE ORDEN
    // ----------------------------

    public async Task SetPreparing(string orderId)
    {
        await _orderRepository.UpdateStatusWithTimeAsync(
            orderId,
            OrderStatus.Preparing,
            DateTime.UtcNow,
            null
        );

        var order = await GetOrderById(orderId);

        if (order != null)
            await _notificationService.NotifyOrderPreparing(order);
    }

    public async Task SetReady(string orderId)
    {
        await _orderRepository.UpdateStatusWithTimeAsync(
            orderId,
            OrderStatus.Ready,
            null,
            DateTime.UtcNow
        );

        var order = await GetOrderById(orderId);

        if (order != null)
            await _notificationService.NotifyOrderReady(order);
    }

    public async Task SetFinished(string orderId)
    {
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Delivered);

        var order = await GetOrderById(orderId);

        if (order != null)
            await _notificationService.NotifyOrderDelivered(order);
    }

    public async Task MarkAsPaid(string orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.Status != OrderStatus.Delivered || order.IsPaid) return;

        await _orderRepository.MarkAsPaidAsync(orderId);

        var updatedOrder = await GetOrderById(orderId);
        if (updatedOrder != null)
            await _notificationService.NotifyOrderPaid(updatedOrder);
    }

    // ----------------------------
    // MESA (CAJA / ADMIN)
    // ----------------------------

    public async Task CloseTable(int tableNumber)
    {
        await _orderRepository.MarkCleanupCompletedForTableAsync(tableNumber);
        await _tableRepository.SetOccupiedAsync(tableNumber, false);
        await _notificationService.NotifyTableStatusUpdated(tableNumber, false);
    }

    // ----------------------------
    // QUERIES
    // ----------------------------

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
        var pendingCleanupOrders = new List<OrderDto>();

        foreach (var order in allOrders
            .Where(o => o.Status == OrderStatus.Delivered && o.IsPaid && !o.IsCleanupCompleted)
            .OrderByDescending(o => o.PaidAt ?? o.DeliveredAt ?? o.CreatedAt))
        {
            var cleanupReferenceDate = order.PaidAt ?? order.DeliveredAt ?? order.CreatedAt;
            var hasNewerOrderForSameTable = await _orderRepository.HasNewerOrdersForTableAsync(
                order.TableNumber,
                cleanupReferenceDate,
                order.Id!
            );

            if (!hasNewerOrderForSameTable)
            {
                pendingCleanupOrders.Add(MapToDto(order));
            }
        }

        return new WaiterSummaryDto
        {
            WaiterId = userId,
            WaiterName = username,
            TotalCreated = allOrders.Count,
            TotalDelivered = allOrders.Count(o => o.Status == OrderStatus.Delivered),
            MyActiveOrders = allOrders
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                .Select(MapToDto)
                .ToList(),
            PendingCleanupOrders = pendingCleanupOrders
        };
    }

    public async Task CancelOrder(string orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) return;

        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);

        // Restaurar stock
        foreach (var item in order.Items)
        {
            await _productRepository.RestoreStockAsync(item.ProductId, item.Quantity);

            var updated = await _productRepository.GetByIdAsync(item.ProductId);

            if (updated != null)
                await _notificationService.NotifyStockUpdated(item.ProductId, updated.Stock);
        }

        // Verificar si la mesa queda libre
        var stillActive = await _orderRepository.HasActiveOrdersForTableAsync(order.TableNumber, orderId);

        if (!stillActive)
        {
            await _tableRepository.SetOccupiedAsync(order.TableNumber, false);
            await _notificationService.NotifyTableStatusUpdated(order.TableNumber, false);
        }
        var orderDto = MapToDto(order);
        await _notificationService.NotifyOrderCancelled(orderDto);
    }

    // ----------------------------
    // GETTERS
    // ----------------------------

    public async Task<OrderDto?> GetOrderById(string id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order != null ? MapToDto(order) : null;
    }

    public async Task<List<OrderDto>> GetActiveOrders() =>
        (await _orderRepository.GetActiveOrdersAsync())
            .Select(MapToDto)
            .ToList();

    public async Task<List<OrderDto>> GetHistory() =>
        (await _orderRepository.GetHistoryAsync())
            .Select(MapToDto)
            .ToList();

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

    // ----------------------------
    // MAPPER
    // ----------------------------

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id!,
        TableNumber = order.TableNumber,
        CustomerName = order.CustomerName,
        WaiterName = order.WaiterName,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        StartedAt = order.StartedAt,
        ReadyAt = order.ReadyAt,
        DeliveredAt = order.DeliveredAt,
        IsPaid = order.IsPaid,
        PaidAt = order.PaidAt,
        IsCleanupCompleted = order.IsCleanupCompleted,
        CleanupCompletedAt = order.CleanupCompletedAt,
        Items = order.Items?.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Notes = i.Notes,
            CurrentStock = 0
        }).ToList() ?? []
    };
}
