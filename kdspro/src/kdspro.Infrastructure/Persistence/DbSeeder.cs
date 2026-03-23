using kdspro.Domain.Entities;
using MongoDB.Driver;

namespace kdspro.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedProducts(IMongoCollection<Product> collection)
    {
        if (await collection.CountDocumentsAsync(_ => true) > 0) return;

        var wendysMenu = new List<Product>
        {
            new() { Name = "Dave's Single",            Description = "Cuarto de libra de carne fresca con queso",   Price = 5.99m,  Category = "Hamburguesas",    Stock = 2,   ImageName = "daves-single.png"            },
            new() { Name = "Dave's Double",            Description = "Media libra de carne fresca con queso",       Price = 7.49m,  Category = "Hamburguesas",    Stock = 50,  ImageName = "daves-double.png"            },
            new() { Name = "Baconator",                Description = "Doble carne, doble queso y mucho tocino",     Price = 8.99m,  Category = "Hamburguesas",    Stock = 50,  ImageName = "baconator.png"               },
            new() { Name = "Son of Baconator",         Description = "Versión junior del clásico Baconator",        Price = 6.25m,  Category = "Hamburguesas",    Stock = 50,  ImageName = "son-of-baconator.png"        },
            new() { Name = "Spicy Chicken Sandwich",   Description = "Pechuga de pollo picante empanizada",         Price = 5.49m,  Category = "Pollo",           Stock = 50,  ImageName = "spicy-chicken-sandwich.png"  },
            new() { Name = "Classic Chicken Sandwich", Description = "Pechuga de pollo clásica empanizada",         Price = 5.19m,  Category = "Pollo",           Stock = 50,  ImageName = "classic-chicken-sandwich.png"},
            new() { Name = "10 PC. Spicy Nuggets",     Description = "Nuggets de pollo picantes",                   Price = 4.99m,  Category = "Pollo",           Stock = 50,  ImageName = "spicy-nuggets.png"           },
            new() { Name = "10 PC. Crispy Nuggets",    Description = "Nuggets de pollo crujientes",                 Price = 4.99m,  Category = "Pollo",           Stock = 50,  ImageName = "crispy-nuggets.png"          },
            new() { Name = "Natural-Cut Fries (L)",    Description = "Papas fritas con sal de mar",                 Price = 3.25m,  Category = "Acompañamientos", Stock = 100, ImageName = "natural-cut-fries.png"       },
            new() { Name = "Baconator Fries",          Description = "Papas con queso derretido y tocino",          Price = 4.50m,  Category = "Acompañamientos", Stock = 100, ImageName = "baconator-fries.png"         },
            new() { Name = "Chili Cheese Fries",       Description = "Papas con chili y queso",                     Price = 4.25m,  Category = "Acompañamientos", Stock = 100, ImageName = "chili-cheese-fries.png"      },
            new() { Name = "Classic Chili (L)",        Description = "El famoso chili de Wendy's",                  Price = 3.99m,  Category = "Acompañamientos", Stock = 100, ImageName = "classic-chili.png"           },
            new() { Name = "Baked Potato w/ Cheese",   Description = "Papa horneada con queso",                     Price = 3.50m,  Category = "Acompañamientos", Stock = 100, ImageName = "baked-potato.png"            },
            new() { Name = "Chocolate Frosty (L)",     Description = "Postre lácteo congelado sabor chocolate",     Price = 2.99m,  Category = "Postres",         Stock = 200, ImageName = "chocolate-frosty.png"        },
            new() { Name = "Vanilla Frosty (L)",       Description = "Postre lácteo congelado sabor vainilla",      Price = 2.99m,  Category = "Postres",         Stock = 200, ImageName = "vanilla-frosty.png"          },
            new() { Name = "Coca-Cola (L)",            Description = "Bebida carbonatada grande",                   Price = 2.50m,  Category = "Bebidas",         Stock = 500, ImageName = "coca-cola.png"               },
            new() { Name = "Lemonade (L)",             Description = "Limonada fresca natural",                     Price = 2.75m,  Category = "Bebidas",         Stock = 500, ImageName = "lemonade.png"                },
            new() { Name = "Iced Tea (L)",             Description = "Té frío sin azúcar",                          Price = 2.25m,  Category = "Bebidas",         Stock = 500, ImageName = "iced-tea.png"                },
            new() { Name = "Cold Brew Coffee",         Description = "Café frío artesanal",                         Price = 3.50m,  Category = "Bebidas",         Stock = 500, ImageName = "cold-brew-coffee.png"        },
            new() { Name = "Apple Pecan Salad",        Description = "Ensalada fresca con manzana y nueces",        Price = 7.99m,  Category = "Ensaladas",       Stock = 50,  ImageName = "apple-pecan-salad.png"       },
        };

        await collection.InsertManyAsync(wendysMenu);
    }

    public static async Task SeedTables(IMongoCollection<Table> collection)
    {
        if (await collection.CountDocumentsAsync(_ => true) > 0) return;

        var tables = new List<Table>
        {
            new() { Number = 1, Name = "Mesa 1", Capacity = 4, IsActive = true },
            new() { Number = 2, Name = "Mesa 2", Capacity = 4, IsActive = true },
            new() { Number = 3, Name = "Mesa 3", Capacity = 4, IsActive = true },
            new() { Number = 4, Name = "Mesa 4", Capacity = 6, IsActive = true },
            new() { Number = 5, Name = "Mesa 5", Capacity = 2, IsActive = true },
            new() { Number = 6, Name = "Mesa 6", Capacity = 8, IsActive = true }
        };

        await collection.InsertManyAsync(tables);
    }

    public static async Task SeedUsers(IMongoCollection<User> users)
    {
        var count = await users.CountDocumentsAsync(_ => true);
        if (count > 0) return;

        // CREDENCIALES DE ACCESO:
        // admin   → Admin_KDS_2026!
        // kitchen → kitchen2026
        // waiter  → waiter2026
        await users.InsertManyAsync(new List<User>
        {
            new() { Username = "admin",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin_KDS_2026!"), Role = "admin"   },
            new() { Username = "kitchen", PasswordHash = BCrypt.Net.BCrypt.HashPassword("kitchen2026"),     Role = "kitchen" },
            new() { Username = "waiter",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("waiter2026"),      Role = "waiter"  },
        });
    }
}
