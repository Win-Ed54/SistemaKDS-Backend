using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Domain.Enums;
using kdspro.Api.Hubs; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Controllers;

/// <summary>
/// Controlador principal para la gestión de órdenes (Módulo KDS - Mes 2).
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
    /// Es el endpoint que consulta la pantalla de cocina al cargar para mostrar los tickets.
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<Order>>> Get(CancellationToken ct) 
    {
        var orders = await _repository.GetActiveOrdersAsync(ct); 
        return Ok(orders);
    }

    /// <summary>
    /// Crea una nueva orden y notifica instantáneamente a la pantalla de la cocina.
    /// Punto de entrada desde la terminal móvil del mesero (Mes 3).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Order order)
    {
        // 1. Persistencia atómica en MongoDB
        await _repository.CreateAsync(order);

        // 2. COMUNICACIÓN REAL-TIME: El ticket aparece en la cocina sin refrescar (F5)
        await _hubContext.Clients.Group("cocina").SendAsync("ReceiveOrder", order);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// NUEVO: Endpoint específico para el flujo de despacho del KDS.
    /// Cambia el estado de 'Preparing' a 'Ready' y notifica a los meseros para el pickup.
    /// </summary>
    /// <param name="id">ID único de la orden en MongoDB.</param>
    [HttpPatch("{id}/ready")]
    public async Task<IActionResult> MarkAsReady(string id, CancellationToken ct)
    {
        // 1. Validación de integridad
        var existingOrder = await _repository.GetByIdAsync(id);
        if (existingOrder == null)
        {
            return NotFound(new { message = $"La orden con ID {id} no existe" });
        }
        
        // 2. ACTUALIZACIÓN CRÍTICA: Cambia el estado a Ready (1) en MongoDB
        // Esto hace que la orden deje de aparecer como 'Preparing' en Compass
        await _repository.UpdateStatusAsync(id, OrderStatus.Ready, ct);

        // 3. REAL-TIME COCINA: La tarjeta desaparece del panel KDS instantáneamente
        await _hubContext.Clients.Group("cocina").SendAsync("UpdateOrderStatus", id, "Ready");
        
        // 4. FLUJO DE SERVICIO: Alerta global para que el mesero recoja el pedido en mesa
        await _hubContext.Clients.All.SendAsync("NotifyWaiterOrderReady", new 
        { 
            OrderId = id, 
            Table = existingOrder.TableNumber,
            Customer = existingOrder.CustomerName
        });
        
        return Ok(new { message = "Orden lista para servir y persistida en DB" });
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
}
