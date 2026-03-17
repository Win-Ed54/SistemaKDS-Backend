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

var builder = WebApplication.CreateBuilder(args);

// --- LOGGING ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- CONTROLADORES ---
builder.Services.AddControllers()
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.Converters.Add(
        new Newtonsoft.Json.Converters.StringEnumConverter()
    );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
       Name = "Authorization",
       Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
       Scheme = "bearer",
       BearerFormat = "JWT",
       In = Microsoft.OpenApi.Models.ParameterLocation.Header,
       Description = "Ingresa el token JWT aqui:"  
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string [] {}
        }
    });
});

// ---JWT AUTHENTICATION ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        )
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
           var accessToken = context.Request.Query["access_token"];

           var path = context.HttpContext.Request.Path;
           if(!string.IsNullOrEmpty(accessToken) &&
           path.StartsWithSegments("/ordersHub"))
            {
                context.Token = accessToken;
            } 

            return Task.CompletedTask;
        }
    };    
});



builder.Services.AddAuthorization();

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
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient("mongodb://mongodb:27017");
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("kdspro");
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();



var app = builder.Build();

// --- MIDDLEWARE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// --- SIGNALR HUB ---
app.MapHub<OrdersHub>("/ordersHub");

// --- SEED DE BASE DE DATOS ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<MongoDbContext>();

        await DbSeeder.SeedProducts(context.Products);
        await DbSeeder.SeedTables(context.Tables);
        await DbSeeder.SeedUsers(context.Users);

        Console.WriteLine(">>> Base de datos poblada correctamente");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> Error al sembrar datos: {ex.Message}");
    }
}

app.Run();
