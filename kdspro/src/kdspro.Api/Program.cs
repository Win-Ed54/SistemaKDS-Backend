using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Repositories;
using kdspro.Infrastructure.Persistence;
using kdspro.Api.Hubs; 

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE CONTROLADORES Y SERIALIZACIÓN ---
// Se añade NewtonsoftJson para personalizar cómo los datos viajan al Frontend (React).
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    // CONFIGURACIÓN CRÍTICA: Los estados (Enums) se envían como texto ("Pending", "Ready") 
    // en lugar de números (0, 1). Esto hace que el Frontend sea mucho más fácil de programar.
    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter()); 
});

// Configuración de Swagger para la documentación interactiva de la API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. COMUNICACIÓN EN TIEMPO REAL (SIGNALR) ---
// Habilita las capacidades de WebSockets necesarias para que la cocina reciba tickets sin F5.
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

// IMPORTANTE: El CORS debe aplicarse antes de los controladores para evitar bloqueos.
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

// Registro de las rutas de los Controladores (API REST).
app.MapControllers(); 

// --- 6. ENDPOINT DE TIEMPO REAL ---
// Define la ruta "/ordersHub" como el punto de entrada para los WebSockets del KDS.
app.MapHub<OrdersHub>("/ordersHub");

app.Run();
