using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using kdspro.Api.Hubs;
using kdspro.Application.Services;
using kdspro.Application.DTOs;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    private readonly IProductRepository _productRepository;
    private readonly IHubContext<OrdersHub> _hub;
    private readonly IWebHostEnvironment _environment;
    private readonly IIngredientRepository _ingredientRepository;

    public ProductsController(
        IProductRepository productRepository,
        IHubContext<OrdersHub> hub,
        IWebHostEnvironment environment,
        IIngredientRepository ingredientRepository)
    {
        _productRepository = productRepository;
        _hub = hub;
        _environment = environment;
        _ingredientRepository = ingredientRepository;
    }

    [Authorize(Roles = "waiter,admin,kitchen")]
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        var products = await _productRepository.GetAllAsync();
        var ingredients = await _ingredientRepository.GetAllAsync();
        var ingredientsById = ingredients
            .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient.Id))
            .ToDictionary(ingredient => ingredient.Id!, ingredient => ingredient, StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            IngredientAvailabilityService.ApplyAvailability(product, ingredientsById);
        }

        return Ok(products);
    }

    [Authorize(Roles = "waiter,admin,kitchen")]
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(string id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return NotFound(new { message = "Producto no encontrado" });

        var ingredients = await _ingredientRepository.GetAllAsync();
        var ingredientsById = ingredients
            .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient.Id))
            .ToDictionary(ingredient => ingredient.Id!, ingredient => ingredient, StringComparer.OrdinalIgnoreCase);
        IngredientAvailabilityService.ApplyAvailability(product, ingredientsById);

        return Ok(product);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] Product product)
    {
        var validationError = ValidateProduct(product);
        if (validationError != null) return BadRequest(new { message = validationError });

        await _productRepository.CreateAsync(product);
        await NotifyProductCatalogUpdated();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("upload-image")]
    [RequestSizeLimit(MaxImageSizeBytes)]
    public async Task<ActionResult> UploadImage([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Selecciona una imagen valida." });
        if (file.Length > MaxImageSizeBytes)
            return BadRequest(new { message = "La imagen no puede superar 5 MB." });

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            return BadRequest(new { message = "Solo se permiten archivos JPG, PNG o WEBP." });

        var uploadsPath = GetProductImagesDirectory();
        Directory.CreateDirectory(uploadsPath);

        var safeName = SlugifyFileName(Path.GetFileNameWithoutExtension(file.FileName));
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{safeName}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        return Ok(new { imageUrl = $"/images/productos/{fileName}" });
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Product product)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (string.IsNullOrWhiteSpace(product.ImageUrl) || product.ImageUrl == "default.webp")
        {
            product.ImageUrl = existing.ImageUrl;
        }

        if (product.Recipe == null || product.Recipe.Count == 0)
        {
            product.Recipe = existing.Recipe ?? new List<ProductRecipeItem>();
        }

        var validationError = ValidateProduct(product);
        if (validationError != null) return BadRequest(new { message = validationError });

        product.Id = id;
        await _productRepository.UpdateAsync(id, product);
        DeleteProductImageIfReplaced(existing.ImageUrl, product.ImageUrl);
        await NotifyProductCatalogUpdated();

        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _productRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        await _productRepository.DeleteAsync(id);
        DeleteProductImage(existing.ImageUrl);
        await NotifyProductCatalogUpdated();

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
            await _hub.Clients.Groups("waiter", "admin").SendAsync("stockupdated", id, dto.NewStock);

            return Ok(new { id, newStock = dto.NewStock });
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>> Error actualizando stock: {ex.Message}");
            return StatusCode(500, new { error = "No se pudo actualizar el stock." });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id}/recipe")]
    public async Task<IActionResult> UpdateRecipe(string id, [FromBody] UpdateProductRecipeDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return NotFound(new { message = "Producto no encontrado" });

        var ingredients = await _ingredientRepository.GetAllAsync();
        var ingredientsById = ingredients.ToDictionary(
            ingredient => ingredient.Id ?? string.Empty,
            ingredient => ingredient,
            StringComparer.OrdinalIgnoreCase);

        var recipe = new List<ProductRecipeItem>();
        foreach (var item in dto.Items ?? [])
        {
            if (!ingredientsById.TryGetValue(item.IngredientId, out var ingredient) || ingredient == null)
                return BadRequest(new { message = "Uno de los ingredientes seleccionados ya no existe." });

            recipe.Add(new ProductRecipeItem
            {
                IngredientId = ingredient.Id ?? string.Empty,
                IngredientName = ingredient.Name,
                Unit = ingredient.Unit,
                QuantityRequired = item.QuantityRequired,
            });
        }

        await _productRepository.UpdateRecipeAsync(id, recipe);
        await NotifyProductCatalogUpdated();
        return NoContent();
    }

    private async Task NotifyProductCatalogUpdated()
    {
        await _hub.Clients.Groups("waiter", "admin").SendAsync("productupdated");
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

    private string GetProductImagesDirectory()
    {
        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
            : _environment.WebRootPath;

        return Path.Combine(webRootPath, "images", "productos");
    }

    private void DeleteProductImageIfReplaced(string? previousImageUrl, string? newImageUrl)
    {
        if (string.Equals(previousImageUrl, newImageUrl, StringComparison.OrdinalIgnoreCase)) return;
        DeleteProductImage(previousImageUrl);
    }

    private void DeleteProductImage(string? imageUrl)
    {
        if (!IsSafeImageUrl(imageUrl) || string.Equals(imageUrl, "default.webp", StringComparison.OrdinalIgnoreCase))
            return;

        var fileName = Path.GetFileName(imageUrl);
        if (string.IsNullOrWhiteSpace(fileName)) return;

        var imagePath = Path.Combine(GetProductImagesDirectory(), fileName);
        if (System.IO.File.Exists(imagePath))
        {
            System.IO.File.Delete(imagePath);
        }
    }

    private static string SlugifyFileName(string rawValue)
    {
        var normalized = rawValue.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = Regex.Replace(builder.ToString(), "[^a-zA-Z0-9-_]+", "-").Trim('-').ToLowerInvariant();
        return string.IsNullOrWhiteSpace(slug) ? "producto" : slug;
    }
}
