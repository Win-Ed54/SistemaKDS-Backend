using kdspro.Application.DTOs;
using kdspro.Api.Hubs;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IngredientsController : ControllerBase
{
    private readonly IIngredientRepository _ingredientRepository;
    private readonly IHubContext<OrdersHub> _hubContext;

    public IngredientsController(IIngredientRepository ingredientRepository, IHubContext<OrdersHub> hubContext)
    {
        _ingredientRepository = ingredientRepository;
        _hubContext = hubContext;
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var ingredients = await _ingredientRepository.GetAllAsync();
        return Ok(ingredients.OrderBy(ingredient => ingredient.Name));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] IngredientDto dto)
    {
        var ingredient = new Ingredient
        {
            Name = dto.Name.Trim(),
            Unit = dto.Unit.Trim(),
            Stock = dto.Stock,
            MinimumStock = dto.MinimumStock,
            IsActive = dto.IsActive,
        };

        await _ingredientRepository.CreateAsync(ingredient);
        await NotifyProductCatalogUpdated();
        return Ok(ingredient);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(string id, [FromBody] IngredientDto dto)
    {
        var existing = await _ingredientRepository.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Ingrediente no encontrado." });

        existing.Name = dto.Name.Trim();
        existing.Unit = dto.Unit.Trim();
        existing.Stock = dto.Stock;
        existing.MinimumStock = dto.MinimumStock;
        existing.IsActive = dto.IsActive;

        await _ingredientRepository.UpdateAsync(id, existing);
        await NotifyProductCatalogUpdated();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _ingredientRepository.GetByIdAsync(id);
        if (existing == null) return NotFound(new { message = "Ingrediente no encontrado." });

        await _ingredientRepository.DeleteAsync(id);
        await NotifyProductCatalogUpdated();
        return NoContent();
    }

    private async Task NotifyProductCatalogUpdated()
    {
        await _hubContext.Clients.Groups("waiter", "admin").SendAsync("productupdated");
        await _hubContext.Clients.Groups("waiter", "admin").SendAsync("ProductUpdated");
    }
}
