using System;
using System.Collections.Generic;
using System.Linq;
using kdspro.Application.DTOs;
using kdspro.Domain.Entities;
using kdspro.Domain.Enums;

namespace kdspro.Application.Mappings;

public static class OrderMapping
{
    // DTO -> Entity
    public static Order ToEntity(CreateOrderDto dto)
    {
        return new Order
        {
            TableNumber = dto.TableNumber,
            CustomerName = dto.CustomerName,
            TakeoutDestination = dto.TakeoutDestination,
            WaiterName = dto.WaiterName,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,

            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Modifiers = i.Modifiers ?? new List<string>(),
                Notes = i.Notes ?? ""
            }).ToList()
        };
    }

    // Entity -> DTO
    public static OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id!,
            TableNumber = order.TableNumber,
            CustomerName = order.CustomerName,
            TakeoutDestination = order.TakeoutDestination,
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

