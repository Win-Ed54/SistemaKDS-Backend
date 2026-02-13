
using Microsoft.AspNetCore.Mvc;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")] // La URL será: api/products
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    // Le pedimos al sistema que nos dé el repositorio que configuramos antes
    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    // 1. Endpoint para ver todo el menú
    [HttpGet]
    public async Task<ActionResult<List<Product>>> Get()
    {
        var products = await _repository.GetAllAsync();
        return Ok(products);
    }

    // 2. Endpoint para crear un nuevo platillo (Ej: Hamburguesa)
    [HttpPost]
    public async Task<IActionResult> Post(Product product)
    {
        await _repository.CreateAsync(product);
        return Ok(new { message = "Producto creado exitosamente" });
    }
}