using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Domain.Enums;
using kdspro.Api.Hubs; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Controllers;

/// <summary>
/// Controlador principal para la gestión de órdenes (Módulo KDS & Mesero).
/// Orquesta la persistencia en MongoDB y las notificaciones en tiempo real vía SignalR.
/// </summary>
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
    /// Obtiene todas las órdenes activas (Pendientes y en Preparación).
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<Order>>> Get(CancellationToken ct) 
    {
        var orders = await _repository.GetActiveOrdersAsync(ct); 
        return Ok(orders);
    }

    /// <summary>
    /// Consulta el detalle de una orden específica por su ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetById(string id)
    {
        var order = await _repository.GetByIdAsync(id);
        return order != null ? Ok(order) : NotFound();
    }

    /// <summary>
    /// Punto de entrada desde la terminal móvil del mesero (Mes 3).
    /// Crea una nueva orden y notifica instantáneamente a la cocina (ReceiveOrder).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Order order)
    {
        await _repository.CreateAsync(order);
        
        // SignalR: Envía al grupo "cocina" para mostrar el ticket sin F5
        await _hubContext.Clients.Group("cocina").SendAsync("ReceiveOrder", order);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Endpoint para el flujo de despacho del KDS (Mes 2).
    /// Cambia el estado a 'Ready' y emite la alerta sonora/visual para el pickup del mesero.
    /// </summary>
    [HttpPatch("{id}/ready")]
    public async Task<IActionResult> MarkAsReady(string id, CancellationToken ct)
    {
        var existingOrder = await _repository.GetByIdAsync(id);
        if (existingOrder == null) return NotFound(new { message = "Orden no encontrada" });
        
        await _repository.UpdateStatusAsync(id, OrderStatus.Ready, ct);

        // Notifica a cocina para remover ticket y a meseros para recoger
        await _hubContext.Clients.Group("cocina").SendAsync("UpdateOrderStatus", id, "Ready");
        await _hubContext.Clients.Group("waiters").SendAsync("NotifyWaiterOrderReady", new 
        { 
            OrderId = id, 
            Table = existingOrder.TableNumber,
            Customer = existingOrder.CustomerName
        });
        
        return Ok(new { message = "Orden lista para servir." });
    }

    /// <summary>
    /// NUEVO: Endpoint para la entrega final por parte del mesero (Mes 3).
    /// Cierra el ciclo de vida del pedido (Ready -> Finished) y registra auditoría.
    /// </summary>
    [HttpPatch("{id}/finish")]
    public async Task<IActionResult> MarkAsFinished(string id, CancellationToken ct)
    {
        var existingOrder = await _repository.GetByIdAsync(id);
        if (existingOrder == null) return NotFound();

        // 1. Persistencia: Cambia estado a Finished (2) y guarda fecha de finalización
        await _repository.UpdateStatusAsync(id, OrderStatus.Delivered, ct);

        // 2. Real-time: Notifica a todos los clientes para limpiar cualquier alerta residual
        await _hubContext.Clients.All.SendAsync("OrderFinalized", id);

        return Ok(new { message = "Orden entregada con éxito." });
    }
}