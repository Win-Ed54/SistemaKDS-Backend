
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

    public OrdersController(
        IOrderService orderService,
        IHubContext<OrdersHub> hub)
    {
        _orderService = orderService;
        _hub = hub;
    }

    // Crear orden
    [Authorize(Roles = "waiter,admin")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrder(dto);

        await _hub.Clients.Group("kitchen")
            .SendAsync("ReceiveOrder", order);

        return Ok(order);
    }

    // Obtener órdenes activas
    [Authorize(Roles = "kitchen,admin")]
    [HttpGet("active")]
    public async Task<ActionResult<List<OrderDto>>> GetActiveOrders()
    {
        var orders = await _orderService.GetActiveOrders();
        return Ok(orders);
    }

    // Obtener órdenes listas
    [Authorize(Roles = "kitchen,admin")]
    [HttpGet("ready")]
    public async Task<ActionResult<List<OrderDto>>> GetReadyOrders()
    {
        var orders = await _orderService.GetReadyOrders();
        return Ok(orders);
    }

    // Obtener historial
    [Authorize(Roles = "kitchen,admin")]
    [HttpGet("history")]
    public async Task<ActionResult<List<OrderDto>>> GetHistory()
    {
        var orders = await _orderService.GetHistory();
        return Ok(orders);
    }

    // Cambiar a preparing
    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/preparing")]
    public async Task<IActionResult> Preparing(string id)
    {
        await _orderService.SetPreparing(id);

        var order = await _orderService.GetOrderById(id);

        if (order == null)
            return NotFound();

        await _hub.Clients.Group("kitchen")
            .SendAsync("OrderPreparing", order);

        return Ok(order);
    }

    // Cambiar a ready
    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/ready")]
    public async Task<IActionResult> Ready(string id)
    {
        await _orderService.SetReady(id);

        var order = await _orderService.GetOrderById(id);

        if (order == null)
            return NotFound();

        await _hub.Clients.All
            .SendAsync("OrderReady", order);

        return Ok(order);
    }

    // Finalizar orden
    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/finish")]
    public async Task<IActionResult> Finish(string id)
    {
        await _orderService.SetFinished(id);

        await _hub.Clients.All
            .SendAsync("OrderDelivered", id);

        return Ok(new {id, message = "Orden entregada con exito"});
    }

    // Cancelar orden
    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        await _orderService.CancelOrder(id);

        await _hub.Clients.All
            .SendAsync("OrderCancelled", id);

        return NoContent();
    }
}
