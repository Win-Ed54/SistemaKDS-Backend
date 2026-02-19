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

    // GET: api/orders/active
    [HttpGet("active")]
    public async Task<ActionResult<List<Order>>> Get(CancellationToken ct) 
    {
        // Trae órdenes pendientes o en preparación para la pantalla inicial
        var orders = await _repository.GetActiveOrdersAsync(ct); 
        return Ok(orders);
    }

    // POST: api/orders
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Order order)
    {
        // 1. Guardar en MongoDB
        await _repository.CreateAsync(order);

        // 2. REAL-TIME: Notificar a la cocina. 
        // Eliminamos 'ct' de SendAsync porque SignalR no lo acepta ahí.
        // Enviamos el objeto 'order' completo para que el frontend tenga el 'CreatedAt'.
        await _hubContext.Clients.Group("cocina").SendAsync("ReceiveOrder", order);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // PATCH: api/orders/{id}/status
    // Usamos PATCH porque solo estamos actualizando una propiedad (el estado)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] OrderStatus status, CancellationToken ct)
    {
        // 1. Validar Existencia
        var existingOrder = await _repository.GetByIdAsync(id);
        if (existingOrder == null)
        {
            return NotFound(new { message = $"La orden con ID {id} no existe" });
        }
        
        // 2. Actualización en DB (Limpia fechas si cambia de Ready a Cooking)
        await _repository.UpdateStatusAsync(id, status, ct);

        // 3. REAL-TIME: Notificar el cambio. 
        // Usamos status.ToString() para que el frontend reciba "Ready" en vez de "2".
        await _hubContext.Clients.Group("cocina").SendAsync("UpdateOrderStatus", id, status.ToString());
        
        // 4. FLUJO CRÍTICO: Notificar al mesero si el pedido está listo
        if (status == OrderStatus.Ready)
        {
            await _hubContext.Clients.All.SendAsync("NotifyWaiterOrderReady", new 
            { 
                OrderId = id, 
                Table = existingOrder.TableNumber // Útil para que el mesero sepa a dónde ir
            });
        }
        
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(string id)
    {
        var order = await _repository.GetByIdAsync(id);
        return order != null ? Ok(order) : NotFound();
    }
}