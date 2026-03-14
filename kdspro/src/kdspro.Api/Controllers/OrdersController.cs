
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using kdspro.Application.Interfaces;
using kdspro.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;

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
    try
    {
        // 1. Se crea la orden y el servicio descuenta el stock en la DB
        var order = await _orderService.CreateOrder(dto);

        // 2. Notificar a la COCINA que hay una nueva orden
        await _hub.Clients.Group("kitchen")
            .SendAsync("receiveorder", order);

        // 3. NOTIFICAR A LOS MESEROS EL NUEVO STOCK (Lo que faltaba)
        // Recorremos los ítems de la orden recién creada
        foreach (var item in order.Items)
        {
            // Enviamos el evento 'stockupdated' con el ID del producto y su NUEVO stock
            // Asegúrate de que tu OrderDetailDto tenga la propiedad 'CurrentStock' o similar
            await _hub.Clients.Group("waiter")
                .SendAsync("stockupdated", item.ProductId, item.CurrentStock); 
        }

        return Ok(order);
    }
    catch (Exception ex)
    {
        // Aquí es donde cae el error de "Stock insuficiente" que vimos antes
        return BadRequest(new { error = ex.Message });
    }
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
