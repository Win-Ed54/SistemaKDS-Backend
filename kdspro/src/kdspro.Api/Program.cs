using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Repositories;
using kdspro.Infrastructure.Persistence;
using kdspro.Api.Hubs;
using kdspro.Application.Services;
using kdspro.Application.Interfaces;
using MongoDB.Driver;
using kdspro.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- LOGGING ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- CONTROLADORES CON NEWTONSOFT ---
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    /* ... Tu configuración de Swagger actual es correcta ... */
});

// --- JWT AUTHENTICATION (Optimizado para SignalR) ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true, // Es mejor validarlo para estabilidad real
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            // Verifica que la petición vaya al Hub de órdenes
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ordersHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };    
});

builder.Services.AddAuthorization();

// --- SIGNALR ESTABLE ---
// NOTA: Requiere el paquete NuGet: Microsoft.AspNetCore.SignalR.Protocols.NewtonsoftJson
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddNewtonsoftJsonProtocol(options => 
{
    // El nombre correcto es PayloadSerializerSettings
    options.PayloadSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    options.PayloadSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
});
// --- CORS ---
var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() 
    ?? new[] { "http://localhost:5173", "http://localhost:5174" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // En desarrollo: permitir cualquier origen
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            // En producción: solo tu dominio
            policy.WithOrigins("https://kdstest.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

//--conexion Temporal
var connectionString = builder.Configuration["MongoDbSettings:ConnectionString"] 
    ?? "mongodb://localhost:27017";

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(connectionString));

var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"] 
    ?? "kdspro";

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

//builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
builder.Services.AddScoped<MongoDbContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();

var app = builder.Build();

// --- MIDDLEWARE PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();

app.UseCors("AllowAll"); // CORS siempre antes de Auth

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrdersHub>("/ordersHub");

// --- SEEDER ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try {
        var context = services.GetRequiredService<MongoDbContext>();
        await DbSeeder.SeedProducts(context.Products);
        await DbSeeder.SeedTables(context.Tables);
        await DbSeeder.SeedUsers(context.Users);
        Console.WriteLine(">>> Datos inicializados correctamente");
    } catch (Exception ex) {
        Console.WriteLine($">>> Error en Seeder: {ex.Message}");
    }
}



app.Run();
