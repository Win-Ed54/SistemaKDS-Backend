using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TablesController : ControllerBase
{
    private readonly ITableRepository _repository;

    public TablesController(ITableRepository repository)
    {
        _repository = repository;
    }

    [Authorize(Roles = "waiter,admin,cashier")]
    [HttpGet]
    public async Task<ActionResult<List<Table>>> GetAll()
    {
        var tables = await _repository.GetAllAsync();
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
}
