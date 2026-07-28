using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Interfaces;
using System.Security.Claims;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
/// <summary>
/// Expone operaciones de pedido y aplica restricciones por rol antes
/// de delegar la logica fuerte al servicio de aplicacion.
/// </summary>
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ITableRepository _tableRepository;
    private readonly IUserRepository _userRepository;
    /// <summary>
    /// Alcances operativos validos para meseros. Se normalizan para evitar
    /// rechazos por mayusculas, espacios o datos heredados.
    /// </summary>
    private static readonly HashSet<string> ValidServiceScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "dining",
        "takeout",
        "hybrid",
    };

    public OrdersController(IOrderService orderService, ITableRepository tableRepository, IUserRepository userRepository)
    {
        _orderService = orderService;
        _tableRepository = tableRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Crea una orden nueva aplicando reglas de asignacion por rol, mesa y
    /// alcance del mesero antes de delegar el alta al servicio.
    /// </summary>
    [Authorize(Roles = "waiter,admin")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value?.Trim().ToLowerInvariant() ?? string.Empty;
        var serviceScope = "hybrid";

        try
        {
            if (role == "waiter")
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { error = "No se pudo identificar al mesero." });

                var currentUser = await _userRepository.GetById(userId);
                if (currentUser == null)
                    return Unauthorized(new { error = "El perfil del mesero ya no existe." });

                serviceScope = NormalizeServiceScope(currentUser.ServiceScope);
                var hasDedicatedTakeoutWaiter = await _userRepository.HasWaiterWithServiceScope("takeout", userId);

                if (serviceScope == "dining" && dto.TableNumber == 0)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = "Este mesero solo puede tomar ordenes de mesas asignadas."
                    });
                }

                if (serviceScope == "takeout" && dto.TableNumber > 0)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = "Este mesero solo puede tomar ordenes para llevar."
                    });
                }

                if (serviceScope != "takeout" && dto.TableNumber == 0 && hasDedicatedTakeoutWaiter)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = "Ya existe un mesero asignado a para llevar. Los demás solo pueden atender sus mesas asignadas o enviar pedidos para llevar desde una mesa asignada."
                    });
                }
            }

            // Las ordenes de mesa solo pueden salir de mesas sentadas y asignadas.
            if (dto.TableNumber > 0)
            {
                var table = await _tableRepository.GetByNumberAsync(dto.TableNumber);
                if (table == null)
                    return NotFound(new { error = "La mesa no existe." });

                if (!table.IsOccupied)
                    return BadRequest(new
                    {
                        error = $"La mesa {dto.TableNumber} no tiene comensales asignados por host."
                    });

                if (role == "waiter")
                {
                    var waiterMatchesById =
                        !string.IsNullOrWhiteSpace(table.AssignedWaiterId) &&
                        table.AssignedWaiterId == userId;
                    var waiterMatchesByName =
                        !string.IsNullOrWhiteSpace(table.AssignedWaiterName) &&
                        string.Equals(
                            table.AssignedWaiterName.Trim(),
                            username?.Trim(),
                            StringComparison.OrdinalIgnoreCase);

                    if (!waiterMatchesById && !waiterMatchesByName)
                        return StatusCode(StatusCodes.Status403Forbidden, new
                        {
                            error = $"La mesa {dto.TableNumber} esta asignada a otro mesero."
                        });
                }
            }

            var order = await _orderService.CreateOrder(dto, userId!, username!);

            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/preparing")]
    /// <summary>
    /// Marca una orden como en preparacion y registra quien la tomo en cocina.
    /// </summary>
    public async Task<IActionResult> Preparing(string id)
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Cocina";
        await _orderService.SetPreparing(id, username);
        var order = await _orderService.GetOrderById(id);
        if (order == null) return NotFound();

        return Ok(order);
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpPatch("{id}/ready")]
    /// <summary>
    /// Marca la orden como lista para despacho y devuelve el estado actualizado.
    /// </summary>
    public async Task<IActionResult> Ready(string id)
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Cocina";
        await _orderService.SetReady(id, username);
        var order = await _orderService.GetOrderById(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [Authorize(Roles = "waiter,admin")]
    [HttpPatch("{id}/finish")]
    /// <summary>
    /// Confirma que la orden ya fue entregada al cliente.
    /// </summary>
    public async Task<IActionResult> Finish(string id)
    {
        await _orderService.SetFinished(id);
        return Ok(new { id, message = "Orden entregada con exito" });
    }

    [Authorize(Roles = "admin,cashier")]
    [HttpPatch("{id}/pay")]
    /// <summary>
    /// Registra el cobro parcial o total de una orden entregada.
    /// </summary>
    public async Task<IActionResult> Pay(string id, [FromBody] MarkOrderPaidDto dto)
    {
        try
        {
            dto ??= new MarkOrderPaidDto();
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Caja";
            await _orderService.MarkAsPaid(id, username, dto);
            return Ok(new { id, message = "Orden cobrada con exito" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/cancel")]
    /// <summary>
    /// Cancela una orden activa y dispara la restitucion de inventario.
    /// </summary>
    public async Task<IActionResult> Cancel(string id)
    {
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Admin";
        await _orderService.CancelOrder(id, username);
        return NoContent();
    }

    [Authorize(Roles = "kitchen,admin")]
    [HttpGet("active")]
    /// <summary>
    /// Devuelve las ordenes visibles para la cola activa de cocina.
    /// </summary>
    public async Task<ActionResult<List<OrderDto>>> GetActiveOrders() =>
        Ok(await _orderService.GetActiveOrders());

    [Authorize(Roles = "admin,cashier")]
    [HttpGet("history")]
    /// <summary>
    /// Devuelve el historial operativo incluyendo ventas listas para cobro.
    /// </summary>
    public async Task<ActionResult<List<OrderDto>>> GetHistory() =>
        Ok(await _orderService.GetHistory());

    [Authorize(Roles = "admin")]
    [HttpGet("top-products")]
    /// <summary>
    /// Calcula un ranking simple de productos vendidos a partir del historial.
    /// </summary>
    public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
    {
        var history = await _orderService.GetHistory();

        var topProducts = history
            .Where(o => (int)o.Status == 3)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                productId = g.Key.ProductId,
                productName = g.Key.ProductName,
                totalSold = g.Sum(i => i.Quantity),
                totalOrders = g.Count()
            })
            .OrderByDescending(x => x.totalSold)
            .Take(limit)
            .ToList();

        return Ok(topProducts);
    }

    [Authorize(Roles = "waiter")]
    [HttpGet("my")]
    /// <summary>
    /// Lista las ordenes activas del mesero autenticado.
    /// </summary>
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var orders = await _orderService.GetMyOrders(userId);
        return Ok(orders);
    }

    [Authorize(Roles = "waiter,admin")]
    [HttpGet("waiter/{waiterName}/today")]
    /// <summary>
    /// Obtiene las ordenes del dia para un mesero. Si el llamador es mesero,
    /// siempre se usa su identidad autenticada aunque la ruta traiga otro valor.
    /// </summary>
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetWaiterOrdersToday(string waiterName)
    {
        try
        {
            var role = User.FindFirstValue(ClaimTypes.Role)?.Trim().ToLowerInvariant() ?? string.Empty;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var lookupValue = role == "waiter" ? userId : waiterName;

            if (string.IsNullOrWhiteSpace(lookupValue))
                return Unauthorized();

            var orders = await _orderService.GetWaiterOrdersToday(lookupValue);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize(Roles = "waiter,admin")]
    [HttpPatch("table/{tableNumber}/close")]
    /// <summary>
    /// Cierra el ciclo de servicio de una mesa una vez que no quedan ordenes,
    /// pagos ni limpieza pendiente.
    /// </summary>
    public async Task<IActionResult> CloseTable(int tableNumber)
    {
        try
        {
            var role = User.FindFirstValue(ClaimTypes.Role)?.Trim().ToLowerInvariant() ?? string.Empty;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? string.Empty;
            var table = await _tableRepository.GetByNumberAsync(tableNumber);

            if (table == null)
                return NotFound(new { error = "La mesa no existe." });

            if (role == "waiter")
            {
                var waiterMatchesById =
                    !string.IsNullOrWhiteSpace(table.AssignedWaiterId) &&
                    table.AssignedWaiterId == userId;
                var waiterMatchesByName =
                    !string.IsNullOrWhiteSpace(table.AssignedWaiterName) &&
                    string.Equals(
                        table.AssignedWaiterName.Trim(),
                        username.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                if (!waiterMatchesById && !waiterMatchesByName)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        error = $"La mesa {tableNumber} esta asignada a otro mesero."
                    });
                }
            }

            await _orderService.CloseTable(tableNumber, userId, role == "admin");
            return Ok(new { message = $"Mesa {tableNumber} liberada correctamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static string NormalizeServiceScope(string? serviceScope)
    {
        // "hybrid" es el comportamiento seguro por defecto cuando el dato no existe.
        var normalized = string.IsNullOrWhiteSpace(serviceScope)
            ? "hybrid"
            : serviceScope.Trim().ToLowerInvariant();

        return ValidServiceScopes.Contains(normalized) ? normalized : "hybrid";
    }
}
