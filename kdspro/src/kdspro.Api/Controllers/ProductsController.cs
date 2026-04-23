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
        var validationError = ValidateProduct(product);
        if (validationError != null) return BadRequest(new { message = validationError });

        await _productRepository.CreateAsync(product);

        //Notificar a meseros que el catálogo cambió
        await _hub.Clients.Groups("waiter", "admin").SendAsync("productupdated");

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
        var validationError = ValidateProduct(product);
        if (validationError != null) return BadRequest(new { message = validationError });

        //Preservar el Id para que MongoDB no lo pierda
        product.Id = id;
        await _productRepository.UpdateAsync(id, product);

        //Notificar a meseros que el catálogo cambió
        await _hub.Clients.Groups("waiter", "admin").SendAsync("productupdated");

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
        await _hub.Clients.Groups("waiter", "admin").SendAsync("productupdated");

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
            if (dto.NewStock < 0 || dto.NewStock > 100000)
                return BadRequest(new { message = "El stock debe estar entre 0 y 100000." });

            await _productRepository.UpdateStockAsync(id, dto.NewStock);

            // Notificar stock actualizado a meseros y admin
            await _hub.Clients.Groups("waiter", "admin").SendAsync("stockupdated", id, dto.NewStock);

            return Ok(new { id, newStock = dto.NewStock });
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> Error actualizando stock: {ex.Message}");
            return StatusCode(500, new { error = "No se pudo actualizar el stock." });
        }
    }

    private static string? ValidateProduct(Product product)
    {
        if (product == null) return "Producto invalido.";
        if (string.IsNullOrWhiteSpace(product.Name) || product.Name.Length > 80)
            return "El nombre del producto es obligatorio y no puede exceder 80 caracteres.";
        if (product.Description?.Length > 300)
            return "La descripcion no puede exceder 300 caracteres.";
        if (string.IsNullOrWhiteSpace(product.Category) || product.Category.Length > 60)
            return "La categoria es obligatoria y no puede exceder 60 caracteres.";
        if (product.Price < 0 || product.Price > 100000)
            return "El precio debe estar entre 0 y 100000.";
        if (product.Stock < 0 || product.Stock > 100000)
            return "El stock debe estar entre 0 y 100000.";
        if (!IsSafeImageUrl(product.ImageUrl))
            return "La imagen debe ser una ruta local valida.";

        product.Name = product.Name.Trim();
        product.Description = product.Description?.Trim() ?? string.Empty;
        product.Category = product.Category.Trim();
        product.ImageUrl = string.IsNullOrWhiteSpace(product.ImageUrl)
            ? "default.webp"
            : product.ImageUrl.Trim();
        return null;
    }

    private static bool IsSafeImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl == "default.webp") return true;
        if (imageUrl.Contains("..", StringComparison.Ordinal)) return false;
        return imageUrl.StartsWith("/images/productos/", StringComparison.OrdinalIgnoreCase);
    }
}
