using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;

    public ProductsController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _productRepository.GetAllAsync();
        return Ok(products);
    }

    // GET: api/products/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult> Create(Product product)
    {
        await _productRepository.CreateAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT: api/products/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Product product)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.UpdateAsync(id, product);
        return NoContent();
    }

    // PATCH: api/products/{id}/availability (Para el stock básico)
    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateAvailability(string id, [FromBody] bool isAvailable)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.UpdateAvailabilityAsync(id, isAvailable);
        return NoContent();
    }

    // DELETE: api/products/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.DeleteAsync(id);
        return NoContent();
    }
}