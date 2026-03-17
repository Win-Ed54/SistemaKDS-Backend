using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Application.Interfaces;
using kdspro.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IHubContext<OrdersHub> _hub;

    public OrdersController(IOrderService orderService, IHubContext<OrdersHub> hub)
    {
        _orderService = orderService;
        _hub = hub;
    }

    [Authorize(Roles = "waiter,admin")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateOrder(dto);

            // 1. NOTIFICAR A COCINA Y ADMIN (Nueva orden entra al sistema)
            await _hub.Clients.Group("kitchen").SendAsync("receiveorder", order);
            await _hub.Clients.Group("admin").SendAsync("ordercreated", order);

            // 2. ACTUALIZAR STOCK EN TIEMPO REAL PARA MESEROS
            foreach (var item in order.Items)
            {
                await _hub.Clients.Group("waiter")
                    .SendAsync("stockupdated", item.ProductId, item.CurrentStock); 
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/preparing")]
    public async Task<IActionResult> Preparing(string id)
    {
        // IMPORTANTE: El servicio debe guardar 'StartedAt = DateTime.UtcNow'
        await _orderService.SetPreparing(id);
        var order = await _orderService.GetOrderById(id);
        if (order == null) return NotFound();

        // NOTIFICAR: Cocina mueve tarjeta, Admin cambia estado a "Cocinando"
        await _hub.Clients.Group("kitchen").SendAsync("orderpreparing", order);
        await _hub.Clients.Group("admin").SendAsync("orderpreparing", order);

        return Ok(order);
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/ready")]
    public async Task<IActionResult> Ready(string id)
    {
        // IMPORTANTE: El servicio debe guardar 'ReadyAt = DateTime.UtcNow'
        await _orderService.SetReady(id);
        var order = await _orderService.GetOrderById(id);
        if (order == null) return NotFound();

        // NOTIFICAR A TODOS: Mesero recibe alerta, Admin calcula EFICIENCIA (ReadyAt - StartedAt)
        await _hub.Clients.All.SendAsync("orderready", order);

        return Ok(order);
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/finish")]
    public async Task<IActionResult> Finish(string id)
    {
        await _orderService.SetFinished(id);

        // NOTIFICAR: Limpia KDS y libera "CAPACIDAD DE SALÓN" en Admin
        await _hub.Clients.All.SendAsync("orderdelivered", id);
        await _hub.Clients.Group("admin").SendAsync("tablesupdated");

        return Ok(new { id, message = "Orden entregada con éxito" });
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        await _orderService.CancelOrder(id);

        // NOTIFICAR: Elimina de todas las pantallas y libera mesa
        await _hub.Clients.All.SendAsync("ordercancelled", id);
        await _hub.Clients.Group("admin").SendAsync("tablesupdated");

        return NoContent();
    }

    // --- GETTERS ---
    [Authorize(Roles = "kitchen,admin")]
    [HttpGet("active")]
    public async Task<ActionResult<List<OrderDto>>> GetActiveOrders() => Ok(await _orderService.GetActiveOrders());
}
