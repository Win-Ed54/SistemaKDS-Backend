using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Repositories;
using kdspro.Infrastructure.Persistence;
using kdspro.Api.Hubs; 

var builder = WebApplication.CreateBuilder(args);

// --- SERVICIOS ---
builder.Services.AddControllers().AddNewtonsoftJson(options =>
     {
        //Configuracion para que los estados no se muestren como numero sino como string pending o ready.
        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter()); 
     });
     
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. AGREGAR SIGNALR
builder.Services.AddSignalR();

// 3. CONFIGURAR CORS (Vital para que React pueda conectarse)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://tu-kds-app.vercel.app") // URL de tu frontend (Vite/React) temporales
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Obligatorio para SignalR
    });
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();

var app = builder.Build();

// --- PIPELINE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. USAR LA POLÍTICA DE CORS (Antes de MapControllers)
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers(); 

// 5. MAPEAR EL HUB (El punto de entrada para el socket)
app.MapHub<OrdersHub>("/ordersHub");

app.Run();