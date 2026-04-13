using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using kdspro.Application.DTOs;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IHubContext<OrdersHub> _hub;

    public ProductsController(IProductRepository productRepository, IHubContext<OrdersHub> hub)
    {
        _productRepository = productRepository;
        _hub = hub;
    }

    [Authorize(Roles = "waiter,admin,kitchen")]
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _productRepository.GetAllAsync();
        return Ok(products);
    }

    [Authorize(Roles = "waiter,admin,kitchen")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return NotFound(new { message = "Producto no encontrado" });
        return Ok(product);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] Product product)
    {
        await _productRepository.CreateAsync(product);

        //Notificar a meseros que el catálogo cambió
        await _hub.Clients.Group("waiter").SendAsync("productupdated");

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Product product)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (string.IsNullOrEmpty(product.ImageUrl) || product.ImageUrl == "default.webp") 
    {
        product.ImageUrl = existing.ImageUrl;
    }
        //Preservar el Id para que MongoDB no lo pierda
        product.Id = id;
        await _productRepository.UpdateAsync(id, product);

        //Notificar a meseros que el catálogo cambió
        await _hub.Clients.All.SendAsync("productupdated");

        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.DeleteAsync(id);

        //Notificar a meseros que el catálogo cambió
        await _hub.Clients.Group("waiter").SendAsync("productupdated");

        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateAvailability(string id, [FromBody] bool isAvailable)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.UpdateAvailabilityAsync(id, isAvailable);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(string id, [FromBody] StockUpdateDto dto)
    {
        try
        {
            var existing = await _productRepository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            await _productRepository.UpdateStockAsync(id, dto.NewStock);

            // Notificar stock actualizado a meseros y admin
            await _hub.Clients.All.SendAsync("stockupdated", id, dto.NewStock);

            return Ok(new { id, newStock = dto.NewStock });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
