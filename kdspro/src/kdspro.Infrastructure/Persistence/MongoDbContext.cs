using MongoDB.Driver;
using kdspro.Domain.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver.Core.Configuration;

namespace kdspro.Infrastructure.Persistence;

/// <summary>
/// Contexto de base de datos para MongoDB (Capa Infrastructure).
/// Actúa como el punto central de conexión y gestión de colecciones para el sistema KDS.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Acceso directo a la base de datos de MongoDB configurada.
    /// </summary>
    public IMongoDatabase Database => _database;

    /// <summary>
    /// Constructor que inicializa la conexión usando el patrón de Inyección de Configuración.
    /// Lee las credenciales y el nombre de la base de datos desde appsettings.json.
    /// </summary>
    /// <param name="configuration">Interfaz para acceder a las variables de entorno o archivos de configuración.</param>
    public MongoDbContext(IConfiguration configuration)
    {
        // 1. OBTENCIÓN DE CONFIGURACIÓN: Extraemos los valores del archivo appsettings.json
        // Esto permite cambiar de base de datos local (Docker) a una de producción sin cambiar el código.
        var connectionString = configuration["MongoDB:ConnectionString"]
                                                 ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"]
                                                 ?? "KDS";
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        // 2. CONEXIÓN AL CLIENTE: Creamos el cliente de MongoDB (Singleton recomendado internamente por el Driver)
        var client = new MongoClient(settings);
        
        // 3. SELECCIÓN DE DB: Referenciamos la base de datos específica (Ej: KdsDatabase)
        _database = client.GetDatabase(databaseName);
    }

    // --- DEFINICIÓN DE COLECCIONES (EQUIVALENTE A TABLAS SQL) ---

    /// <summary>
    /// Colección para el catálogo de productos (Menú, Precios, Stock Crítico).
    /// </summary>
    public IMongoCollection<Product> Products => _database.GetCollection<Product>("Products");

    /// <summary>
    /// Colección principal para el flujo del KDS (Pedidos, Tiempos FIFO, Estados).
    /// </summary>
    public IMongoCollection<Order> Orders => _database.GetCollection<Order>("Orders");

    /// <summary>
    /// Colección de mesas del restaurante.
    /// Permite al mesero seleccionar dónde se asignará una orden.
    /// </summary>
    public IMongoCollection<Table> Tables => _database.GetCollection<Table>("Tables");

    public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

    public IMongoCollection<KdsSettings> KdsSettings => _database.GetCollection<KdsSettings>("KdsSettings");

}
