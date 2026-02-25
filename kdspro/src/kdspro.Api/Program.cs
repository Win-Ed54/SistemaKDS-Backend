using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Repositories;
using kdspro.Infrastructure.Persistence;
using kdspro.Api.Hubs;
using kdspro.Domain.Entities;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONTROLADORES Y SERIALIZACIÓN JSON ---
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter()); 
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- 2. COMUNICACIÓN EN TIEMPO REAL (SIGNALR) ---
// Agregamos el servicio base
builder.Services.AddSignalR();

// --- 3. CONFIGURACIÓN DE SEGURIDAD (CORS) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5174") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

// --- 4. INYECCIÓN DE DEPENDENCIAS ---
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();

var app = builder.Build();

// --- 5. PIPELINE DE LA APLICACIÓN (MIDDLEWARES) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers(); 

// --- 6. ENDPOINT DE TIEMPO REAL CON CONFIGURACIÓN DE .NET 8 ---
// Movimos la configuración aquí para evitar el error de compilación CS1061
app.MapHub<OrdersHub>("/ordersHub", options => 
{
    options.AllowStatefulReconnects = true;
});

// --- 7. SIEMBRA AUTOMÁTICA DE DATOS ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MongoDbContext>();
        await DbSeeder.SeedProducts(context.Products);
        Console.WriteLine(">>> Base de datos poblada con éxito con el Menú de Wendy's.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> Error en la siembra de base de datos: {ex.Message}");
    }
}

app.Run();
