namespace kdspro.Domain.Entities;

public class Product
{
    // El ID será un string para que sea fácil de usar con MongoDB
    public string Id { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    
    // Para saber si el plato está disponible (ej. si se acabó el pollo)
    public bool IsAvailable { get; set; } = true;
    
    // Categoría para organizar: "Hamburguesas", "Bebidas"
    public string Category { get; set; } = string.Empty;
}
/*Este es un objeto simple (POCO). Solo guarda datos. No hace cálculos todavía.
*/