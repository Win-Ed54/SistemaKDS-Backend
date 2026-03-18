using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using kdspro.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using kdspro.Application.DTOs;


namespace kdspro.Api.Controllers;

/// <summary>
/// Controlador para la gestión del catálogo de productos (Módulo de Menú - Mes 1).
/// Permite administrar los platillos, bebidas y acompañamientos del restaurante.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IHubContext<OrdersHub> _hub;

    public ProductsController(IProductRepository productRepository, IHubContext<OrdersHub> hub)
    {
        _productRepository = productRepository;
        _hub = hub;
    }

    /// <summary>
    /// Obtiene la lista completa de productos (Menú).
    /// Es el endpoint principal que consultará la terminal del mesero para mostrar opciones al cliente.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _productRepository.GetAllAsync();
        return Ok(products);
    }

    /// <summary>
    /// Busca un producto específico por su identificador único de MongoDB.
    /// </summary>
    /// <param name="id">ID del producto (ObjectId).</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return NotFound(new { message = "Producto no encontrado" });
        return Ok(product);
    }

    /// <summary>
    /// Registra un nuevo producto en el catálogo (Módulo Admin).
    /// Permite añadir lanzamientos temporales o nuevos platos al menú de Wendy's.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] Product product)
    {
        await _productRepository.CreateAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Actualiza la información completa de un producto existente.
    /// Se utiliza para cambios de nombre, descripción o ajustes de precio.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Product product)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        product.Id = id;

        await _productRepository.UpdateAsync(id, product);
        return NoContent();
    }

    /// <summary>
    /// Gestión de Stock Crítico: Activa o desactiva la disponibilidad de un producto.
    /// Si se termina un ingrediente (ej. carne), el Admin lo desactiva aquí para que 
    /// los meseros dejen de ofrecerlo instantáneamente.
    /// </summary>
    /// <param name="id">ID del producto.</param>
    /// <param name="isAvailable">Estado de stock (true/false).</param>
    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateAvailability(string id, [FromBody] bool isAvailable)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.UpdateAvailabilityAsync(id, isAvailable);
        return NoContent();
    }

    /// <summary>
    /// Elimina físicamente un producto del catálogo.
    /// Se recomienda usar con precaución para no romper el historial de órdenes pasadas.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.DeleteAsync(id);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(string id, [FromBody] StockUpdateDto dto)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        // Actualizamos en la Base de Datos
        await _productRepository.UpdateStockAsync(id, dto.NewStock);

        // NOTIFICACIÓN EN TIEMPO REAL:
        // Esto hace que el Admin y el Mesero se actualicen sin F5
        await _hub.Clients.All.SendAsync("stockupdated", id, dto.NewStock);

        return Ok(new { id, newStock = dto.NewStock });
    }
}


