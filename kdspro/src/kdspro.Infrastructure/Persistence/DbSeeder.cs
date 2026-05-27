using kdspro.Application.Services;
using kdspro.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace kdspro.Infrastructure.Persistence;

public static class DbSeeder
{
    private const int DefaultProductStock = 100;

    public static async Task SeedProducts(IMongoCollection<Product> collection)
    {
        var wendysMenu = new List<Product>
        {
            new() { Name = "Dave's Single", Description = "Cuarto de libra de carne fresca con queso", Price = 5.99m, Category = "Hamburguesas", Stock = DefaultProductStock, ImageUrl = "/images/productos/daves-single.webp" },
            new() { Name = "Dave's Double", Description = "Media libra de carne fresca con queso", Price = 7.49m, Category = "Hamburguesas", Stock = DefaultProductStock, ImageUrl = "/images/productos/daves-double.webp" },
            new() { Name = "Baconator", Description = "Doble carne, doble queso y mucho tocino", Price = 8.99m, Category = "Hamburguesas", Stock = DefaultProductStock, ImageUrl = "/images/productos/baconator.webp" },
            new() { Name = "Son of Baconator", Description = "Version junior del clasico Baconator", Price = 6.25m, Category = "Hamburguesas", Stock = DefaultProductStock, ImageUrl = "/images/productos/son-of-baconator.webp" },
            new() { Name = "Spicy Chicken Sandwich", Description = "Pechuga de pollo picante empanizada", Price = 5.49m, Category = "Pollo", Stock = DefaultProductStock, ImageUrl = "/images/productos/spicy-chicken-sandwich.webp" },
            new() { Name = "Classic Chicken Sandwich", Description = "Pechuga de pollo clasica empanizada", Price = 5.19m, Category = "Pollo", Stock = DefaultProductStock, ImageUrl = "/images/productos/classic-chicken-sandwich.webp" },
            new() { Name = "10 PC. Spicy Nuggets", Description = "Nuggets de pollo picantes", Price = 4.99m, Category = "Pollo", Stock = DefaultProductStock, ImageUrl = "/images/productos/spicy-nuggets.webp" },
            new() { Name = "10 PC. Crispy Nuggets", Description = "Nuggets de pollo crujientes", Price = 4.99m, Category = "Pollo", Stock = DefaultProductStock, ImageUrl = "/images/productos/crispy-nuggets.webp" },
            new() { Name = "Natural-Cut Fries (L)", Description = "Papas fritas con sal de mar", Price = 3.25m, Category = "Acompanamientos", Stock = DefaultProductStock, ImageUrl = "/images/productos/natural-cut-fries.webp" },
            new() { Name = "Baconator Fries", Description = "Papas con queso derretido y tocino", Price = 4.50m, Category = "Acompanamientos", Stock = DefaultProductStock, ImageUrl = "/images/productos/baconator-fries.webp" },
            new() { Name = "Chili Cheese Fries", Description = "Papas con chili y queso", Price = 4.25m, Category = "Acompanamientos", Stock = DefaultProductStock, ImageUrl = "/images/productos/chili-cheese-fries.webp" },
            new() { Name = "Classic Chili (L)", Description = "El famoso chili de Wendy's", Price = 3.99m, Category = "Acompanamientos", Stock = DefaultProductStock, ImageUrl = "/images/productos/classic-chili.webp" },
            new() { Name = "Baked Potato w/ Cheese", Description = "Papa horneada con queso", Price = 3.50m, Category = "Acompanamientos", Stock = DefaultProductStock, ImageUrl = "/images/productos/baked-potato.webp" },
            new() { Name = "Chocolate Frosty (L)", Description = "Postre lacteo congelado sabor chocolate", Price = 2.99m, Category = "Postres", Stock = DefaultProductStock, ImageUrl = "/images/productos/chocolate-frosty.webp" },
            new() { Name = "Vanilla Frosty (L)", Description = "Postre lacteo congelado sabor vainilla", Price = 2.99m, Category = "Postres", Stock = DefaultProductStock, ImageUrl = "/images/productos/vanilla-frosty.webp" },
            new() { Name = "Coca-Cola (L)", Description = "Bebida carbonatada grande", Price = 2.50m, Category = "Bebidas", Stock = DefaultProductStock, ImageUrl = "/images/productos/coca-cola.webp" },
            new() { Name = "Lemonade (L)", Description = "Limonada fresca natural", Price = 2.75m, Category = "Bebidas", Stock = DefaultProductStock, ImageUrl = "/images/productos/lemonade.webp" },
            new() { Name = "Iced Tea (L)", Description = "Te frio sin azucar", Price = 2.25m, Category = "Bebidas", Stock = DefaultProductStock, ImageUrl = "/images/productos/iced-tea.webp" },
            new() { Name = "Cold Brew Coffee", Description = "Cafe frio artesanal", Price = 3.50m, Category = "Bebidas", Stock = DefaultProductStock, ImageUrl = "/images/productos/cold-brew-coffee.webp" },
            new() { Name = "Apple Pecan Salad", Description = "Ensalada fresca con manzana y nueces", Price = 7.99m, Category = "Ensaladas", Stock = DefaultProductStock, ImageUrl = "/images/productos/apple-pecan-salad.webp" },
            new() { Name = "Breakfast Baconator", Description = "Croissant con huevo, tocino, salchicha y queso", Price = 6.49m, Category = "Desayunos", Stock = DefaultProductStock, ImageUrl = "/images/productos/baconator.webp" },
            new() { Name = "Honey Butter Biscuit", Description = "Biscuit con pollo y mantequilla con miel", Price = 4.99m, Category = "Desayunos", Stock = DefaultProductStock, ImageUrl = "/images/productos/classic-chicken-sandwich.webp" },
            new() { Name = "Combo Dave's Single", Description = "Hamburguesa, papas y bebida", Price = 8.99m, Category = "Combos de Wendy", Stock = DefaultProductStock, ImageUrl = "/images/productos/daves-single.webp" },
            new() { Name = "Combo Spicy Chicken", Description = "Sandwich de pollo, papas y bebida", Price = 8.49m, Category = "Combos de Wendy", Stock = DefaultProductStock, ImageUrl = "/images/productos/spicy-chicken-sandwich.webp" },
        };

        var existingProducts = await collection.Find(_ => true).ToListAsync();
        if (existingProducts.Count == 0)
        {
            await collection.InsertManyAsync(wendysMenu);
            return;
        }

        foreach (var existing in existingProducts)
        {
            var normalizedCategory = string.Equals(existing.Category, "Acompañamientos", StringComparison.OrdinalIgnoreCase)
                ? "Acompanamientos"
                : existing.Category;

            await collection.UpdateOneAsync(
                product => product.Id == existing.Id,
                Builders<Product>.Update
                    .Set(product => product.Category, normalizedCategory ?? string.Empty));
        }

        foreach (var product in wendysMenu)
        {
            var exists = existingProducts.Any(existing =>
                string.Equals(existing.Name?.Trim(), product.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                await collection.InsertOneAsync(product);
            }
        }
    }

    public static async Task SeedTables(IMongoCollection<Table> collection)
    {
        var tables = new List<Table>
        {
            new() { Number = 1, Name = "Mesa 1", Capacity = 4, IsActive = true },
            new() { Number = 2, Name = "Mesa 2", Capacity = 4, IsActive = true },
            new() { Number = 3, Name = "Mesa 3", Capacity = 4, IsActive = true },
            new() { Number = 4, Name = "Mesa 4", Capacity = 6, IsActive = true },
            new() { Number = 5, Name = "Mesa 5", Capacity = 2, IsActive = true },
            new() { Number = 6, Name = "Mesa 6", Capacity = 8, IsActive = true },
            new() { Number = 7, Name = "Mesa 7", Capacity = 8, IsActive = true },
            new() { Number = 8, Name = "Mesa 8", Capacity = 8, IsActive = true },
            new() { Number = 9, Name = "Mesa 9", Capacity = 10, IsActive = true },
            new() { Number = 10, Name = "Mesa 10", Capacity = 10, IsActive = true },
            new() { Number = 11, Name = "Mesa 11", Capacity = 10, IsActive = true },
            new() { Number = 12, Name = "Mesa 12", Capacity = 10, IsActive = true },
            new() { Number = 13, Name = "Mesa 13", Capacity = 10, IsActive = true },
            new() { Number = 14, Name = "Mesa 14", Capacity = 10, IsActive = true },
            new() { Number = 15, Name = "Mesa 15", Capacity = 10, IsActive = true }
        };

        var existingTables = await collection.Find(_ => true).ToListAsync();
        if (existingTables.Count == 0)
        {
            await collection.InsertManyAsync(tables);
            return;
        }

        foreach (var table in tables)
        {
            var existing = existingTables.FirstOrDefault(item => item.Number == table.Number);
            if (existing == null)
            {
                await collection.InsertOneAsync(table);
                continue;
            }

            await collection.UpdateOneAsync(
                current => current.Id == existing.Id,
                Builders<Table>.Update
                    .Set(current => current.Name, table.Name)
                    .Set(current => current.Capacity, table.Capacity)
                    .Set(current => current.IsActive, table.IsActive));
        }
    }

    public static async Task SeedUsers(IMongoCollection<User> users)
    {
        var seedUsers = new[]
        {
            new { Username = "admin", Password = "Admin_KDS_2026!", Role = "admin", ServiceScope = "hybrid" },
            new { Username = "gerente", Password = "Gerente2026!", Role = "admin", ServiceScope = "hybrid" },
            new { Username = "supervisor", Password = "Supervisor2026!", Role = "admin", ServiceScope = "hybrid" },
            new { Username = "caja1", Password = "Caja2026!", Role = "cashier", ServiceScope = "hybrid" },
            new { Username = "caja2", Password = "Caja22026!", Role = "cashier", ServiceScope = "hybrid" },
            new { Username = "caja3", Password = "Caja32026!", Role = "cashier", ServiceScope = "hybrid" },
            new { Username = "kitchen1", Password = "chef2026", Role = "kitchen", ServiceScope = "hybrid" },
            new { Username = "kitchen2", Password = "preparador2026", Role = "kitchen", ServiceScope = "hybrid" },
            new { Username = "kitchen3", Password = "cocina32026", Role = "kitchen", ServiceScope = "hybrid" },
            new { Username = "waiter1", Password = "waiter2026", Role = "waiter", ServiceScope = "dining" },
            new { Username = "Edwin", Password = "Edwin2026", Role = "waiter", ServiceScope = "hybrid" },
            new { Username = "waiter2", Password = "waiter22026", Role = "waiter", ServiceScope = "takeout" },
            new { Username = "waiter3", Password = "waiter32026", Role = "waiter", ServiceScope = "dining" },
            new { Username = "host1", Password = "host2026", Role = "host", ServiceScope = "hybrid" },
            new { Username = "host2", Password = "host22026", Role = "host", ServiceScope = "hybrid" },
            new { Username = "host3", Password = "host32026", Role = "host", ServiceScope = "hybrid" }
        };

        foreach (var seedUser in seedUsers)
        {
            var usernameFilter = Builders<User>.Filter.Regex(
                user => user.Username,
                new BsonRegularExpression($"^\\s*{Regex.Escape(seedUser.Username)}\\s*$", "i"));

            var existingUser = await users.Find(usernameFilter).FirstOrDefaultAsync();
            if (existingUser == null)
            {
                await users.InsertOneAsync(new User
                {
                    Username = seedUser.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedUser.Password),
                    Role = seedUser.Role,
                    ServiceScope = seedUser.ServiceScope
                });
                continue;
            }

            var update = Builders<User>.Update
                .Set(user => user.Username, seedUser.Username)
                .Set(user => user.Role, seedUser.Role)
                .Set(user => user.ServiceScope, seedUser.ServiceScope);

            if (!PasswordMatches(existingUser.PasswordHash, seedUser.Password))
            {
                update = update.Set(
                    user => user.PasswordHash,
                    BCrypt.Net.BCrypt.HashPassword(seedUser.Password));
            }

            await users.UpdateOneAsync(
                user => user.Id == existingUser.Id,
                update);
        }
    }

    private static bool PasswordMatches(string passwordHash, string password)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) return false;

        if (passwordHash.StartsWith("$2a$", StringComparison.Ordinal) ||
            passwordHash.StartsWith("$2b$", StringComparison.Ordinal) ||
            passwordHash.StartsWith("$2x$", StringComparison.Ordinal) ||
            passwordHash.StartsWith("$2y$", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        return string.Equals(passwordHash, password, StringComparison.Ordinal);
    }

    public static async Task SeedKdsSettings(IMongoCollection<KdsSettings> settingsCollection)
    {
        if (await settingsCollection.CountDocumentsAsync(_ => true) > 0) return;

        var defaults = OrderValidationRules.GetDefaults(OrderValidationRules.QuickServiceMode);

        await settingsCollection.InsertOneAsync(new KdsSettings
        {
            Id = "default",
            ServiceMode = OrderValidationRules.QuickServiceMode,
            MaxDistinctItems = defaults.MaxDistinctItems,
            MaxTotalUnits = defaults.MaxTotalUnits,
            MaxQuantityPerProduct = defaults.MaxQuantityPerProduct,
            LargeOrderUnitsWarning = defaults.LargeOrderUnitsWarning,
            TakeoutRequirePrepayment = false,
            RequireCustomerNameForTakeout = true,
            DefaultCleaningMinutes = 8,
            MaxPartySize = 10,
            MaxTablesPerWaiter = 5,
            RequireConnectedWaitersForAssignment = true,
            UpdatedAt = DateTime.UtcNow,
        });
    }
}
