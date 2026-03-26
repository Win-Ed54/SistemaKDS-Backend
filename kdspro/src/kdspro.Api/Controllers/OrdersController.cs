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
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        try
        {
            var order = await _orderService.CreateOrder(dto, userId!, username!);
            // ✅ Las notificaciones de nueva orden y stock las maneja
            //    OrderService.CreateOrder → NotifyNewOrder + NotifyProductOutOfStock
            //    Solo el stockupdated por waiter va aquí porque el servicio
            //    no tiene acceso directo al grupo "waiter" en este momento
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
        await _orderService.SetPreparing(id);
        var order = await _orderService.GetOrderById(id);
        if (order == null) return NotFound();

        // ✅ Solo notifica a kitchen — admin ya lo recibe en SetPreparing via servicio
        await _hub.Clients.Group("kitchen").SendAsync("orderpreparing", order);

        return Ok(order);
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/ready")]
    public async Task<IActionResult> Ready(string id)
    {
        await _orderService.SetReady(id);
        var order = await _orderService.GetOrderById(id);
        if (order == null) return NotFound();

        // ✅ Notifica a todos (mesero recibe alerta, cocina mueve tarjeta)
        await _hub.Clients.All.SendAsync("orderready", order);

        return Ok(order);
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/finish")]
    public async Task<IActionResult> Finish(string id)
    {
        // ✅ SetFinished ya maneja: liberar mesa + NotifyOrderDelivered
        await _orderService.SetFinished(id);

        // tablesupdated solo lo emite el controller (no está en el servicio)
        await _hub.Clients.Group("admin").SendAsync("tablesupdated");

        return Ok(new { id, message = "Orden entregada con éxito" });
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        // ✅ CancelOrder ya maneja:
        //    - Devolver stock → RestoreStockAsync
        //    - Notificar stock → NotifyStockUpdated (waiter + admin)
        //    - Liberar mesa → SetOccupiedAsync
        //    - Notificar cancelación → NotifyOrderCancelled (All)
        //    - tablesupdated → solo aquí
        await _orderService.CancelOrder(id);
        await _hub.Clients.Group("admin").SendAsync("tablesupdated");

        return NoContent();
    }

    // --- GETTERS ---
    [Authorize(Roles = "kitchen,admin")]
    [HttpGet("active")]
    public async Task<ActionResult<List<OrderDto>>> GetActiveOrders()
        => Ok(await _orderService.GetActiveOrders());

    [Authorize(Roles = "admin")]
    [HttpGet("history")]
    public async Task<ActionResult<List<OrderDto>>> GetHistory()
        => Ok(await _orderService.GetHistory());

    [Authorize(Roles = "admin")]
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
    {
        var history = await _orderService.GetHistory();

        var topProducts = history
            .Where(o => (int)o.Status == 3)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                productId   = g.Key.ProductId,
                productName = g.Key.ProductName,
                totalSold   = g.Sum(i => i.Quantity),
                totalOrders = g.Count()
            })
            .OrderByDescending(x => x.totalSold)
            .Take(limit)
            .ToList();

        return Ok(topProducts);
    }

    [Authorize(Roles = "waiter")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var orders = await _orderService.GetMyOrders(userId);
        return Ok(orders);
    }
    [Authorize(Roles = "waiter,admin")]
    [HttpGet("waiter/{waiterName}/today")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetWaiterOrdersToday(string waiterName)
    {
        try
        {
            var orders = await _orderService.GetWaiterOrdersToday(waiterName);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("table/{tableNumber}/close")]
    public async Task<IActionResult> CloseTable(int tableNumber)
    {
        try
        {
            await _orderService.CloseTable(tableNumber);

            // 📢 Notificamos a TODOS (Meseros y Admin) que la mesa está libre.
            // Usamos el evento 'tablesupdated' que ya escuchas en el frontend.
            await _hub.Clients.All.SendAsync("tablesupdated");

            return Ok(new { message = $"Mesa {tableNumber} liberada correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


}