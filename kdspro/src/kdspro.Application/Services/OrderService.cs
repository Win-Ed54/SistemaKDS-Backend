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
    private readonly IOrderNotificationService _notificationService;

    public OrderService(IOrderRepository orderRepository,
    IProductRepository productRepository,
    IOrderNotificationService notificationService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
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
            // IMPORTANTE: Inicializamos tiempos en null
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
        
        // 3. NOTIFICAR A COCINA Y ADMIN (Para actualización automática sin F5)
        await _notificationService.NotifyNewOrder(order);

        var resultDto = MapToDto(order);
        foreach (var itemDto in resultDto.Items)
        {
            if (updatedStocks.TryGetValue(itemDto.ProductId, out int currentStock))
                itemDto.CurrentStock = currentStock;
        }

        return resultDto;
    }

    // --- ACTUALIZACIÓN DE ESTADOS CON REGISTRO DE TIEMPOS (FIX EFICIENCIA) ---

    public async Task SetPreparing(string orderId)
    {
        // Guardamos la hora de inicio para el cálculo de eficiencia
        var startTime = DateTime.UtcNow;
        await _orderRepository.UpdateStatusWithTimeAsync(orderId, OrderStatus.Preparing, startTime, null); 
        
        // Notificar al Admin para que vea el cambio de color y registre el tiempo de inicio
        var order = await GetOrderById(orderId);
        if(order != null) await _notificationService.NotifyOrderPreparing(order);
    }

    public async Task SetReady(string orderId)
    {
        // Guardamos la hora de finalización
        var readyTime = DateTime.UtcNow;
        await _orderRepository.UpdateStatusWithTimeAsync(orderId, OrderStatus.Ready, null, readyTime);
        
        // Notificar al Admin para que calcule el TIEMPO PROMEDIO (ReadyAt - StartedAt)
        var order = await GetOrderById(orderId);
        if(order != null) await _notificationService.NotifyOrderReady(order);
    }

    public async Task SetFinished(string orderId)
    {
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Delivered);
        
        // Notificar al Admin para liberar la mesa en el Dashboard
        await _notificationService.NotifyOrderDelivered(orderId);
    }

    public async Task CancelOrder(string orderId)
    {
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);
        await _notificationService.NotifyOrderCancelled(orderId);
    }

    // --- MAPPER CON TIEMPOS ---
    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id!,
            TableNumber = order.TableNumber,
            CustomerName = order.CustomerName,
            WaiterName = order.WaiterName,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            
            // ESTOS CAMPOS SON LOS QUE HACEN QUE EL DASHBOARD NO MARQUE 0 MIN
            StartedAt = order.StartedAt, 
            ReadyAt = order.ReadyAt,     

            Items = order.Items?.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Notes = i.Notes,
                CurrentStock = 0 // Se llena en el CreateOrder si es necesario
            }).ToList() ?? new List<OrderItemDto>()
        };
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
}
