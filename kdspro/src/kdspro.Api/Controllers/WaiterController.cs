using System.Security.Claims;
using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kdspro.Api.Controllers;

[Authorize] // Solo usuarios con token válido pueden entrar
[ApiController]
[Route("api/[controller]")]
public class WaiterController : ControllerBase
{
    private readonly IOrderService _orderService;

    public WaiterController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // GET: api/waiter/summary
    [HttpGet("summary")]
    public async Task<ActionResult<WaiterSummaryDto>> GetMySummary()
    {
        // 1. Extraemos el ID del mesero directamente del Token JWT
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Mesero";

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("No se pudo identificar al usuario en el token.");
        }

        // 2. Pedimos el resumen al servicio
        var summary = await _orderService.GetWaiterSummary(userId, userName);
        
        return Ok(summary);
    }

    [HttpGet("today")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrdersToday()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("No se pudo identificar al usuario en el token.");
        }

        var orders = await _orderService.GetWaiterOrdersToday(userId);
        return Ok(orders);
    }

    // GET: api/waiter/my-orders
    // (Opcional) Si solo quieres la lista de órdenes activas sin los contadores
    [HttpGet("my-orders")]
    public async Task<ActionResult<List<OrderDto>>> GetMyActiveOrders()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var orders = await _orderService.GetMyOrders(userId);
        return Ok(orders);
    }
}
