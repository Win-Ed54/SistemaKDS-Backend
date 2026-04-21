using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using kdspro.Application.DTOs;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TablesController : ControllerBase
{
    private const int MaxPartySize = 10;
    private readonly ITableRepository _repository;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderNotificationService _notificationService;

    public TablesController(
        ITableRepository repository,
        IOrderRepository orderRepository,
        IOrderNotificationService notificationService)
    {
        _repository = repository;
        _orderRepository = orderRepository;
        _notificationService = notificationService;
    }

    [Authorize(Roles = "waiter,host,admin,cashier")]
    [HttpGet]
    public async Task<ActionResult<List<Table>>> GetAll()
    {
        var tables = await _repository.GetAllAsync();
        foreach (var table in tables)
        {
            var hasActiveOrders = await _orderRepository.HasActiveOrdersForTableAsync(
                table.Number,
                string.Empty);

            if (hasActiveOrders)
            {
                table.IsOccupied = true;
            }
        }

        return Ok(tables);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Table table)
    {
        await _repository.CreateAsync(table);
        return CreatedAtAction(nameof(GetAll), new { id = table.Id }, table);
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] bool isActive)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "La mesa no existe" });

        await _repository.UpdateAvailabilityAsync(id, isActive);
        return NoContent();
    }

    [Authorize(Roles = "waiter,host,admin")]
    [HttpPatch("{tableNumber:int}/seat")]
    public async Task<IActionResult> SeatGuests(int tableNumber, [FromBody] SeatTableDto dto)
    {
        if (dto.PartySize < 1)
            return BadRequest(new { message = "La cantidad de comensales debe ser mayor a cero." });

        if (dto.PartySize > MaxPartySize)
            return BadRequest(new { message = $"La cantidad de comensales no puede superar {MaxPartySize}." });

        if (dto.EstimatedDiningMinutes < 1)
            return BadRequest(new { message = "El tiempo estimado debe ser mayor a cero." });
        if (string.IsNullOrWhiteSpace(dto.AssignedWaiterId) || string.IsNullOrWhiteSpace(dto.AssignedWaiterName))
            return BadRequest(new { message = "Debes asignar un mesero a la mesa." });

        var table = await _repository.GetByNumberAsync(tableNumber);
        if (table == null) return NotFound(new { message = "La mesa no existe." });
        if (!table.IsActive) return BadRequest(new { message = "La mesa no esta activa." });
        if (table.IsOccupied || await _orderRepository.HasActiveOrdersForTableAsync(tableNumber, string.Empty))
            return BadRequest(new { message = "La mesa no se encuentra libre en este momento." });
        if (table.Capacity > 0 && dto.PartySize > table.Capacity)
            return BadRequest(new { message = $"La mesa {tableNumber} admite maximo {table.Capacity} comensales." });

        var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Anfitrion";

        await _repository.SeatGuestsAsync(
            tableNumber,
            dto.PartySize,
            dto.EstimatedDiningMinutes,
            dto.Notes?.Trim() ?? string.Empty,
            userName,
            dto.AssignedWaiterId.Trim(),
            dto.AssignedWaiterName.Trim(),
            DateTime.UtcNow);

        var updatedTable = await _repository.GetByNumberAsync(tableNumber);
        if (updatedTable == null) return NotFound(new { message = "No se pudo actualizar la mesa." });

        await _notificationService.NotifyTableStatusUpdated(updatedTable);
        return Ok(updatedTable);
    }

    [Authorize(Roles = "host,admin")]
    [HttpPatch("{tableNumber:int}/unseat")]
    public async Task<IActionResult> UnseatGuests(int tableNumber)
    {
        var table = await _repository.GetByNumberAsync(tableNumber);
        if (table == null) return NotFound(new { message = "La mesa no existe." });
        if (!table.IsOccupied)
            return BadRequest(new { message = "La mesa ya se encuentra libre." });
        if (table.IsBeingCleaned)
            return BadRequest(new { message = "No puedes cancelar una mesa en limpieza." });

        if (await _orderRepository.HasActiveOrdersForTableAsync(tableNumber, string.Empty))
        {
            return BadRequest(new
            {
                message = $"La mesa {tableNumber} ya tiene ordenes activas. Ya no puede ser cancelada por host."
            });
        }

        if (await _orderRepository.HasPendingPaymentForTableAsync(tableNumber))
        {
            return BadRequest(new
            {
                message = $"La mesa {tableNumber} tiene cobros pendientes. No puede ser cancelada por host."
            });
        }

        await _repository.ClearServiceStateAsync(tableNumber, false);

        var updatedTable = await _repository.GetByNumberAsync(tableNumber);
        if (updatedTable == null) return NotFound(new { message = "No se pudo actualizar la mesa." });

        await _notificationService.NotifyTableStatusUpdated(updatedTable);
        return Ok(updatedTable);
    }

    [Authorize(Roles = "host,admin")]
    [HttpPatch("{tableNumber:int}/transfer")]
    public async Task<IActionResult> TransferGuests(int tableNumber, [FromBody] TransferTableAssignmentDto dto)
    {
        if (dto.TargetTableNumber <= 0)
            return BadRequest(new { message = "Debes seleccionar una mesa destino valida." });
        if (dto.TargetTableNumber == tableNumber)
            return BadRequest(new { message = "La nueva mesa debe ser diferente a la actual." });

        var sourceTable = await _repository.GetByNumberAsync(tableNumber);
        if (sourceTable == null) return NotFound(new { message = "La mesa origen no existe." });
        if (!sourceTable.IsOccupied)
            return BadRequest(new { message = "La mesa origen ya se encuentra libre." });
        if (sourceTable.IsBeingCleaned)
            return BadRequest(new { message = "No puedes mover una mesa que ya entro en limpieza." });

        if (await _orderRepository.HasActiveOrdersForTableAsync(tableNumber, string.Empty))
        {
            return BadRequest(new
            {
                message = $"La mesa {tableNumber} ya tiene ordenes activas. Ya no puede ser reubicada por host."
            });
        }

        if (await _orderRepository.HasPendingPaymentForTableAsync(tableNumber))
        {
            return BadRequest(new
            {
                message = $"La mesa {tableNumber} tiene cobros pendientes. No puede ser reubicada por host."
            });
        }

        var targetTable = await _repository.GetByNumberAsync(dto.TargetTableNumber);
        if (targetTable == null) return NotFound(new { message = "La mesa destino no existe." });
        if (!targetTable.IsActive)
            return BadRequest(new { message = "La mesa destino no esta activa." });
        if (targetTable.IsOccupied || await _orderRepository.HasActiveOrdersForTableAsync(dto.TargetTableNumber, string.Empty))
            return BadRequest(new { message = "La mesa destino no se encuentra libre en este momento." });

        var partySize = sourceTable.CurrentPartySize ?? 0;
        if (partySize < 1)
            return BadRequest(new { message = "La mesa origen no tiene comensales asignados para mover." });
        if (targetTable.Capacity > 0 && partySize > targetTable.Capacity)
        {
            return BadRequest(new
            {
                message = $"La mesa {dto.TargetTableNumber} admite maximo {targetTable.Capacity} comensales."
            });
        }

        var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "Anfitrion";

        await _repository.SeatGuestsAsync(
            dto.TargetTableNumber,
            partySize,
            sourceTable.EstimatedDiningMinutes ?? 1,
            sourceTable.HostNotes,
            userName,
            sourceTable.AssignedWaiterId,
            sourceTable.AssignedWaiterName,
            sourceTable.OccupiedSince ?? DateTime.UtcNow);

        await _repository.ClearServiceStateAsync(tableNumber, false);

        var updatedSourceTable = await _repository.GetByNumberAsync(tableNumber);
        var updatedTargetTable = await _repository.GetByNumberAsync(dto.TargetTableNumber);

        if (updatedSourceTable != null)
            await _notificationService.NotifyTableStatusUpdated(updatedSourceTable);
        if (updatedTargetTable != null)
            await _notificationService.NotifyTableStatusUpdated(updatedTargetTable);

        return Ok(updatedTargetTable);
    }

    [Authorize(Roles = "waiter,admin")]
    [HttpPatch("{tableNumber:int}/start-cleaning")]
    public async Task<IActionResult> StartCleaning(int tableNumber, [FromBody] StartTableCleaningDto dto)
    {
        var estimatedCleaningMinutes = dto.EstimatedCleaningMinutes <= 0 ? 8 : dto.EstimatedCleaningMinutes;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var username = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? string.Empty;

        var table = await _repository.GetByNumberAsync(tableNumber);
        if (table == null) return NotFound(new { message = "La mesa no existe." });
        if (!table.IsOccupied)
            return BadRequest(new { message = "La mesa ya se encuentra libre." });
        if (table.IsBeingCleaned)
            return BadRequest(new { message = "La limpieza de esta mesa ya fue iniciada." });
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
                    message = $"La mesa {tableNumber} esta asignada a otro mesero."
                });
            }
        }

        await _repository.StartCleaningAsync(tableNumber, estimatedCleaningMinutes, DateTime.UtcNow);

        var updatedTable = await _repository.GetByNumberAsync(tableNumber);
        if (updatedTable == null) return NotFound(new { message = "No se pudo actualizar la mesa." });

        await _notificationService.NotifyTableStatusUpdated(updatedTable);
        return Ok(updatedTable);
    }
}
