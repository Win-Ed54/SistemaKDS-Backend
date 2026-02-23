using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Repositories;
using kdspro.Infrastructure.Persistence;
using kdspro.Api.Hubs;
using kdspro.Domain.Entities;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONTROLADORES Y SERIALIZACIÓN JSON ---
// Configura cómo viajan los datos hacia el Frontend (React).
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    // CONFIGURACIÓN CRÍTICA: Los Enums (Status) se envían como texto ("Pending", "Ready") 
    // en lugar de números (0, 1). Esto facilita enormemente la lógica en el Frontend.
    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter()); 
});

// Configuración de Swagger para la documentación interactiva de la API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. COMUNICACIÓN EN TIEMPO REAL (SIGNALR) ---
// Habilita WebSockets para que la cocina reciba tickets al instante sin refrescar (F5).
builder.Services.AddSignalR();

// --- 3. CONFIGURACIÓN DE SEGURIDAD (CORS) ---
// Vital para que el Frontend (Vite/React) pueda comunicarse con el Backend.
// Sin 'AllowCredentials', SignalR no podrá establecer la conexión de socket.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://tu-kds-app.vercel.app") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

// --- 4. INYECCIÓN DE DEPENDENCIAS (CAPA DE INFRAESTRUCTURA) ---
// Registramos los servicios que se conectan con MongoDB.
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();

var app = builder.Build();

// --- 5. PIPELINE DE LA APLICACIÓN (MIDDLEWARES) ---
// Configuración del entorno de desarrollo (Swagger).
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANTE: CORS debe aplicarse antes de los Controladores y Hubs para evitar bloqueos.
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

// Registro de rutas de la API REST.
app.MapControllers(); 

// --- 6. ENDPOINT DE TIEMPO REAL ---
// Define "/ordersHub" como el punto de entrada para los WebSockets del KDS.
app.MapHub<OrdersHub>("/ordersHub");

// --- 7. SIEMBRA AUTOMÁTICA DE DATOS (MES 1 Y 2) ---
// Creamos un "Scope" temporal para poblar la base de datos al arrancar.
// Esto asegura que el menú de Wendy's esté listo para pruebas de inmediato.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MongoDbContext>();
        
        // Poblar la colección "Products" con 20 artículos de Wendy's si está vacía.
        await DbSeeder.SeedProducts(context.Products);
        
        Console.WriteLine(">>> Base de datos poblada con éxito con el Menú de Wendy's.");
    }
    catch (Exception ex)
    {
        // Evita que la API falle si hay un problema de conexión con Docker/MongoDB al inicio.
        Console.WriteLine($">>> Error en la siembra de base de datos: {ex.Message}");
    }
}

app.Run();