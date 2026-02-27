using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Domain.Enums;
using kdspro.Api.Hubs; 
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

    /// <summary>
    /// Obtiene todas las órdenes activas (Pending + Preparing)
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<Order>>> Get(CancellationToken ct) 
    {
        var orders = await _repository.GetActiveOrdersAsync(ct);
        return Ok(orders);
    }

    /// <summary>
    /// Obtener orden por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Id inválido" });

        var order = await _repository.GetByIdAsync(id, ct);

        return order != null
            ? Ok(order)
            : NotFound(new { message = "Orden no encontrada" });
    }

    /// <summary>
    /// Crear nueva orden
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Order order, CancellationToken ct)
    {
        if (order == null || order.Items == null || !order.Items.Any())
            return BadRequest(new { message = "Orden inválida" });

        order.CreatedAt = DateTime.UtcNow;

        // ✅ IMPORTANTE: FORZAR ESTADO INICIAL
        order.Status = OrderStatus.Pending;

        await _repository.CreateAsync(order, ct);

        await _hubContext.Clients.Group("cocina")
            .SendAsync("ReceiveOrder", order);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Cambiar a Preparing
    /// </summary>
    [HttpPatch("{id}/preparing")]
    public async Task<IActionResult> MarkAsPreparing(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Id inválido" });

        var existingOrder = await _repository.GetByIdAsync(id, ct);

        if (existingOrder == null)
            return NotFound(new { message = "Orden no encontrada" });

        if (existingOrder.Status != OrderStatus.Pending)
            return BadRequest(new { message = "La orden ya no está pendiente." });

        await _repository.UpdateStatusAsync(id, OrderStatus.Preparing, ct);

        await _hubContext.Clients.Group("cocina")
            .SendAsync("UpdateOrderStatus", id, OrderStatus.Preparing.ToString());

        return Ok(new { id, status = OrderStatus.Preparing });
    }

    /// <summary>
    /// Cambiar a Ready
    /// </summary>
    [HttpPatch("{id}/ready")]
    public async Task<IActionResult> MarkAsReady(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Id inválido" });

        var existingOrder = await _repository.GetByIdAsync(id, ct);

        if (existingOrder == null)
            return NotFound(new { message = "Orden no encontrada" });

        if (existingOrder.Status != OrderStatus.Preparing)
            return BadRequest(new { message = "La orden debe estar en preparación." });

        await _repository.UpdateStatusAsync(id, OrderStatus.Ready, ct);

        await _hubContext.Clients.Group("cocina")
            .SendAsync("UpdateOrderStatus", id, OrderStatus.Ready.ToString());

        // 🔔 Notificar a meseros
        await _hubContext.Clients.Group("waiters")
            .SendAsync("NotifyWaiterOrderReady", new
            {
                OrderId = id,
                Table = existingOrder.TableNumber,
                Customer = existingOrder.CustomerName
            });

        return Ok(new { id, status = OrderStatus.Ready });
    }

    /// <summary>
    /// Finalizar orden (Delivered)
    /// </summary>
    [HttpPatch("{id}/finish")]
    public async Task<IActionResult> MarkAsFinished(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Id inválido" });

        var existingOrder = await _repository.GetByIdAsync(id, ct);

        if (existingOrder == null)
            return NotFound(new { message = "Orden no encontrada" });

        if (existingOrder.Status != OrderStatus.Ready)
            return BadRequest(new { message = "La orden no está lista." });

        await _repository.UpdateStatusAsync(id, OrderStatus.Delivered, ct);

        await _hubContext.Clients.All
            .SendAsync("OrderFinalized", id);

        return Ok(new { id, status = OrderStatus.Delivered });
    }

    /// <summary>
    /// Cancelar orden
    /// </summary>
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new { message = "Id inválido" });

        var existingOrder = await _repository.GetByIdAsync(id, ct);

        if (existingOrder == null)
            return NotFound(new { message = "Orden no encontrada" });

        if (existingOrder.Status == OrderStatus.Delivered)
            return BadRequest(new { message = "No se puede cancelar una orden entregada." });

        await _repository.UpdateStatusAsync(id, OrderStatus.Cancelled, ct);

        await _hubContext.Clients.All
            .SendAsync("OrderCancelled", id);

        return Ok(new { id, status = OrderStatus.Cancelled });
    }
}
