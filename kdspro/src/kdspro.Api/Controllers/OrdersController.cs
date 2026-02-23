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
    /// Implementa ordenamiento FIFO basado en la fecha de creación.
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
    /// <param name="order">Objeto de la orden con items, modificadores y detalles de mesa.</param>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Order order)
    {
        // 1. Persistencia atómica en MongoDB
        await _repository.CreateAsync(order);

        // 2. COMUNICACIÓN REAL-TIME: El ticket aparece en la cocina sin refrescar (F5)
        // Se envía el objeto completo para procesar tiempos y modificadores en el frontend
        await _hubContext.Clients.Group("cocina").SendAsync("ReceiveOrder", order);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Actualiza el estado de una orden (Ej: de 'Pending' a 'Ready').
    /// Orquesta la actualización de auditoría (FinishedAt) y notifica a los meseros.
    /// </summary>
    /// <param name="id">ID único de la orden en MongoDB.</param>
    /// <param name="status">Nuevo estado de la orden (Enum).</param>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] OrderStatus status, CancellationToken ct)
    {
        // 1. Validación de integridad
        var existingOrder = await _repository.GetByIdAsync(id);
        if (existingOrder == null)
        {
            return NotFound(new { message = $"La orden con ID {id} no existe" });
        }
        
        // 2. Actualización de estado y tiempos de preparación en DB
        await _repository.UpdateStatusAsync(id, status, ct);

        // 3. REAL-TIME: La pantalla de cocina mueve o actualiza el ticket visualmente
        await _hubContext.Clients.Group("cocina").SendAsync("UpdateOrderStatus", id, status.ToString());
        
        // 4. FLUJO CRÍTICO: Si el pedido está listo, se emite una alerta global para los meseros
        if (status == OrderStatus.Ready)
        {
            await _hubContext.Clients.All.SendAsync("NotifyWaiterOrderReady", new 
            { 
                OrderId = id, 
                Table = existingOrder.TableNumber 
            });
        }
        
        return NoContent();
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
