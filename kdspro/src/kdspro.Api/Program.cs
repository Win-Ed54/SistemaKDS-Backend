using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Repositories;
using kdspro.Infrastructure.Persistence;
using kdspro.Api.Hubs;
using kdspro.Domain.Entities;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// --- LOGGING ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- CONTROLADORES ---
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.Converters.Add(
        new Newtonsoft.Json.Converters.StringEnumConverter()
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- SIGNALR ---
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// --- CORS ---
var allowedOrigins = builder.Configuration
    .GetSection("Cors:Origins")
    .Get<string[]>() 
    ?? new[] { "http://localhost:5173", "http://localhost:5174" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- DEPENDENCIAS ---
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();

var app = builder.Build();

// --- MIDDLEWARE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// --- SIGNALR ---
app.MapHub<OrdersHub>("/ordersHub", options =>
{
    options.AllowStatefulReconnects = true;
});

// --- SEED ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<MongoDbContext>();
        await DbSeeder.SeedProducts(context.Products);
        Console.WriteLine(">>> Base de datos poblada correctamente");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> Error: {ex.Message}");
    }
}

app.Run();
