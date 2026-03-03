using Microsoft.AspNetCore.Mvc;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Controllers;

/// <summary>
/// Controlador para la gestión de mesas (Módulo de Administración - Mes 1).
/// Permite al restaurante organizar sus ubicaciones físicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TablesController : ControllerBase
{
    private readonly ITableRepository _repository;

    public TablesController(ITableRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Obtiene la lista completa de mesas registradas en el sistema.
    /// Útil para que el mesero seleccione una mesa al iniciar una orden.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Table>>> GetAll()
    {
        var tables = await _repository.GetAllAsync();
        return Ok(tables);
    }

    /// <summary>
    /// Registra una nueva mesa en la base de datos de MongoDB.
    /// Permite expandir la capacidad del restaurante (ej: añadir mesas en terraza).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Table table)
    {
        // El repositorio genérico se encarga de la persistencia atómica
        await _repository.CreateAsync(table);

        // Retornamos 201 Created con la ubicación del nuevo recurso
        return CreatedAtAction(nameof(GetAll), new { id = table.Id }, table);
    }

    /// <summary>
    /// Activa o desactiva una mesa para el servicio (Módulo Admin).
    /// Ejemplo: Si una mesa se rompe o está reservada, el Admin la marca como 'isActive: false'
    /// para que los meseros no puedan asignarle nuevos pedidos.
    /// </summary>
    /// <param name="id">ID único de la mesa en MongoDB</param>
    /// <param name="isActive">Estado de disponibilidad (true/false)</param>
    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] bool isActive)
    {
        // 1. Verificamos existencia antes de intentar actualizar
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "La mesa no existe" });

        // 2. Ejecutamos la actualización parcial (PATCH) en la base de datos
        await _repository.UpdateAvailabilityAsync(id, isActive);

        return NoContent();
    }
}