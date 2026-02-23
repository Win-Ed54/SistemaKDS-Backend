using kdspro.Domain.Entities;
using MongoDB.Driver;

namespace kdspro.Infrastructure.Persistence;

/// <summary>
/// Clase de utilidad para la carga de datos iniciales (Data Seeding) en MongoDB.
/// Garantiza que el sistema KDS cuente con un menú base funcional desde el primer despliegue.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Pobla la colección de productos con el catálogo oficial de Wendy's si esta se encuentra vacía.
    /// Crucial para demostraciones, pruebas de desarrollo y validación de tipos de datos.
    /// </summary>
    /// <param name="collection">La colección de MongoDB donde se insertarán los documentos.</param>
    /// <returns>Tarea asincrónica que representa la operación de inserción masiva.</returns>
    public static async Task SeedProducts(IMongoCollection<Product> collection)
    {
        // 1. VERIFICACIÓN DE SEGURIDAD: Solo sembramos si la base de datos está totalmente vacía
        // Esto evita duplicar el menú cada vez que se reinicia el servidor.
        if (await collection.CountDocumentsAsync(_ => true) > 0) return;

        // 2. CATÁLOGO DIGITAL DE WENDY'S: 20 productos organizados por categorías
        // Se utilizan sufijos 'm' para asegurar que los precios se traten como decimales de alta precisión.
        var wendysMenu = new List<Product>
        {
            new() { Name = "Dave's Single", Description = "Cuarto de libra de carne fresca con queso", Price = 5.99m, Category = "Hamburguesas", IsAvailable = true },
            new() { Name = "Dave's Double", Description = "Media libra de carne fresca con queso", Price = 7.49m, Category = "Hamburguesas", IsAvailable = true },
            new() { Name = "Baconator", Description = "Doble carne, doble queso y mucho tocino", Price = 8.99m, Category = "Hamburguesas", IsAvailable = true },
            new() { Name = "Son of Baconator", Description = "Versión junior del clásico Baconator", Price = 6.25m, Category = "Hamburguesas", IsAvailable = true },
            new() { Name = "Spicy Chicken Sandwich", Description = "Pechuga de pollo picante empanizada", Price = 5.49m, Category = "Pollo", IsAvailable = true },
            new() { Name = "Classic Chicken Sandwich", Description = "Pechuga de pollo clásica empanizada", Price = 5.19m, Category = "Pollo", IsAvailable = true },
            new() { Name = "10 PC. Spicy Nuggets", Description = "Nuggets de pollo picantes", Price = 4.99m, Category = "Pollo", IsAvailable = true },
            new() { Name = "10 PC. Crispy Nuggets", Description = "Nuggets de pollo crujientes", Price = 4.99m, Category = "Pollo", IsAvailable = true },
            new() { Name = "Natural-Cut Fries (L)", Description = "Papas fritas con sal de mar", Price = 3.25m, Category = "Acompañamientos", IsAvailable = true },
            new() { Name = "Baconator Fries", Description = "Papas con queso derretido y tocino", Price = 4.50m, Category = "Acompañamientos", IsAvailable = true },
            new() { Name = "Chili Cheese Fries", Description = "Papas con chili y queso", Price = 4.25m, Category = "Acompañamientos", IsAvailable = true },
            new() { Name = "Classic Chili (L)", Description = "El famoso chili de Wendy's", Price = 3.99m, Category = "Acompañamientos", IsAvailable = true },
            new() { Name = "Baked Potato w/ Cheese", Description = "Papa horneada con queso", Price = 3.50m, Category = "Acompañamientos", IsAvailable = true },
            new() { Name = "Chocolate Frosty (L)", Description = "Postre lácteo congelado sabor chocolate", Price = 2.99m, Category = "Postres", IsAvailable = true },
            new() { Name = "Vanilla Frosty (L)", Description = "Postre lácteo congelado sabor vainilla", Price = 2.99m, Category = "Postres", IsAvailable = true },
            new() { Name = "Coca-Cola (L)", Description = "Bebida carbonatada grande", Price = 2.50m, Category = "Bebidas", IsAvailable = true },
            new() { Name = "Lemonade (L)", Description = "Limonada fresca natural", Price = 2.75m, Category = "Bebidas", IsAvailable = true },
            new() { Name = "Iced Tea (L)", Description = "Té frío sin azúcar", Price = 2.25m, Category = "Bebidas", IsAvailable = true },
            new() { Name = "Cold Brew Coffee", Description = "Café frío artesanal", Price = 3.50m, Category = "Bebidas", IsAvailable = true },
            new() { Name = "Apple Pecan Salad", Description = "Ensalada fresca con manzana y nueces", Price = 7.99m, Category = "Ensaladas", IsAvailable = true }
        };

        // 3. PERSISTENCIA MASIVA: Se insertan todos los productos en una sola operación de red
        // Optimizamos el rendimiento del inicio de la aplicación.
        await collection.InsertManyAsync(wendysMenu);
    }
}