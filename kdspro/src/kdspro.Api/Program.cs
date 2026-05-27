using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Repositories;
using kdspro.Infrastructure.Persistence;
using kdspro.Api.Hubs;
using kdspro.Api.Middleware;
using kdspro.Application.Services;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using MongoDB.Driver;
using kdspro.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddSwaggerGen(options => {
    /* ... Tu configuración de Swagger actual es correcta ... */
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Environment.IsDevelopment() ? 20 : 8;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

// --- JWT AUTHENTICATION (Optimizado para SignalR) ---
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT Key not configured. Set Jwt__Key in the environment.");
}

if (!builder.Environment.IsDevelopment() &&
    (Encoding.UTF8.GetByteCount(jwtKey) < 32 ||
     jwtKey.StartsWith("DEV_ONLY_", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("JWT Key must be a production secret of at least 32 bytes.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true, // Es mejor validarlo para estabilidad real
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
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
        },
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = identity.FindFirst("sid")?.Value;

                var roleClaims = identity.FindAll(ClaimTypes.Role).ToList();
                foreach (var roleClaim in roleClaims)
                {
                    identity.RemoveClaim(roleClaim);
                }

                foreach (var role in roleClaims
                    .Select(claim => claim.Value?.Trim().ToLowerInvariant())
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Distinct())
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role!));
                }

                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(sessionId))
                {
                    context.Fail("Session invalid");
                    return Task.CompletedTask;
                }

                var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                return userRepository.GetById(userId).ContinueWith(task =>
                {
                    var user = task.Result;
                    if (user == null ||
                        string.IsNullOrWhiteSpace(user.CurrentSessionId) ||
                        !string.Equals(user.CurrentSessionId, sessionId, StringComparison.Ordinal))
                    {
                        context.Fail("Session replaced");
                    }
                });
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
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
})
.AddNewtonsoftJsonProtocol(options => 
{
    // El nombre correcto es PayloadSerializerSettings
    options.PayloadSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    options.PayloadSerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
});
// --- CORS ---
var allowedOrigins = (
        builder.Configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        ?? builder.Configuration["Cors:OriginsCsv"]?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        ?? builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
        ?? new[] { "http://localhost:5173", "http://localhost:5174" }
    )
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

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
            policy.WithOrigins(allowedOrigins)
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
builder.Services.AddScoped<IKdsSettingsRepository, KdsSettingsRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IKdsSettingsService, KdsSettingsService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IOrderNotificationService, OrderNotificationService>();
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddScoped<IAnalyticsService>(sp =>
{
    var database = sp.GetRequiredService<IMongoDatabase>();
    var ordersCollection = database.GetCollection<Order>("orders");
    return new AnalyticsService(ordersCollection);
});

var app = builder.Build();

// --- MIDDLEWARE PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// --- SECURITY HEADERS MIDDLEWARE ---
app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    
    // CSP Header para mitigación de XSS
    var cspHeader = app.Environment.IsDevelopment()
        ? "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https: blob:"
        : "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https: blob:; font-src 'self' data:";
    
    context.Response.Headers.TryAdd("Content-Security-Policy", cspHeader);
    await next();
});

app.UseForwardedHeaders();
app.UseStaticFiles();

// --- GLOBAL EXCEPTION HANDLER ---
app.UseGlobalExceptionHandler();

app.UseCors("AllowAll"); // CORS siempre antes de Auth

app.UseRateLimiter();
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
        if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Seed:DefaultUsers"))
        {
            await DbSeeder.SeedUsers(context.Users);
        }
        await DbSeeder.SeedKdsSettings(context.KdsSettings);
        Console.WriteLine(">>> Datos inicializados correctamente");
    } catch (Exception ex) {
        Console.WriteLine($">>> Error en Seeder: {ex.Message}");
    }
}



app.Run();
