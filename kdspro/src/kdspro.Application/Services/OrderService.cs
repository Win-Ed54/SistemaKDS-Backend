using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using kdspro.Domain.Enums;
using kdspro.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
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

    // Crear nueva orden
    public async Task<OrderDto> CreateOrder(CreateOrderDto dto)
    {
        // 1. VALIDACIÓN Y DESCUENTO DE STOCK (Bucle solo para stock)
        foreach (var item in dto.Items)
        {
            // Usamos el método atómico del repositorio que ya definimos
            bool success = await _productRepository.DeductStockAsync(item.ProductId, item.Quantity);

            if (!success)
            {
                throw new Exception($"Stock insuficiente para: {item.ProductName}");
            }

            // Notificación si se agotó
            var updatedProduct = await _productRepository.GetByIdAsync(item.ProductId);
            if (updatedProduct != null && updatedProduct.Stock <= 0)
            {
                await _notificationService.NotifyProductOutOfStock(item.ProductId);
            }
        }

        // 2. CREACIÓN DE LA ORDEN (Fuera del bucle)
        var order = new Order
        {
            TableNumber = dto.TableNumber,
            CustomerName = dto.CustomerName,
            WaiterName = dto.WaiterName,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Notes = i.Notes ?? ""
            }).ToList()
        };

        await _orderRepository.CreateAsync(order);

        // 3. NOTIFICAR NUEVA ORDEN A COCINA
        await _notificationService.NotifyNewOrder(order);

        return MapToDto(order);
    }


    // Obtener orden por ID
    public async Task<OrderDto?> GetOrderById(string id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
            return null;

        return MapToDto(order);
    }

    // Obtener órdenes activas
    public async Task<List<OrderDto>> GetActiveOrders()
    {
        var orders = await _orderRepository.GetActiveOrdersAsync();

        return orders.Select(MapToDto).ToList();
    }

    // Obtener órdenes listas
    public async Task<List<OrderDto>> GetReadyOrders()
    {
        var orders = await _orderRepository.GetReadyOrdersAsync();

        return orders.Select(MapToDto).ToList();
    }

    // Obtener historial
    public async Task<List<OrderDto>> GetHistory()
    {
        var orders = await _orderRepository.GetHistoryAsync();

        return orders.Select(MapToDto).ToList();
    }

    // Cambiar estado a Preparing
    public async Task SetPreparing(string orderId)
    {
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Preparing);
    }

    // Cambiar estado a Ready
    public async Task SetReady(string orderId)
    {
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Ready);
    }

    // Cambiar estado a Finished
    public async Task SetFinished(string orderId)
    {
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Delivered);
    }

    // Cancelar orden
    public async Task CancelOrder(string orderId)
    {
        await _orderRepository.UpdateStatusAsync(orderId, OrderStatus.Cancelled);
    }

    // Mapper interno
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

        Items = order.Items?.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            Notes = i.Notes,
            Modifiers = i.Modifiers
        }).ToList() ?? new List<OrderItemDto>()
    };
}

}
