using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Domain.Enums;
using kdspro.Api.Hubs; // Importante para SignalR
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;


namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repository;
    private readonly IHubContext<OrdersHub> _hubContext;

    public OrdersController(IOrderRepository repository, IHubContext<OrdersHub> hubContext)
    {
        _repository = repository;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<Order>>> Get()
    {
        var orders = await _repository.GetAllAsync();
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Post(Order order)
    {
        await _repository.CreateAsync(order);

        // ¡ESTO ES LO QUE ACTUALIZA LA COCINA EN TIEMPO REAL!
        await _hubContext.Clients.Group("cocina").SendAsync("ReceiveOrder", order);

        return Ok(new { message = "Orden enviada a cocina", id = order.Id });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] OrderStatus status)
    {
        await _repository.UpdateStatusAsync(id, status);
        
        // Notificamos a todos que el pedido cambió (ej: de Pendiente a Preparando)
        await _hubContext.Clients.Group("cocina").SendAsync("UpdateOrderStatus", id, status);
        
        return NoContent();
    }
}