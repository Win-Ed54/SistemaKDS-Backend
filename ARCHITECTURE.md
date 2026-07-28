# Arquitectura Backend

## Capas

- `kdspro.Api`
  Entrada HTTP, configuracion, hubs y middleware.
- `kdspro.Application`
  Casos de uso, DTOs y reglas de negocio.
- `kdspro.Infrastructure`
  Persistencia MongoDB, seed y repositorios.
- `kdspro.Domain`
  Entidades, enums e interfaces.

## Flujo de una request

1. `Program.cs` registra auth, CORS, SignalR, rate limiting y dependencias.
2. Un controller valida rol, contexto y formato basico.
3. Un servicio de aplicacion ejecuta reglas de negocio.
4. Un repositorio persiste cambios en MongoDB.
5. `OrderNotificationService` emite eventos a los clientes que deban enterarse.

## Archivos clave

- `kdspro/src/kdspro.Api/Program.cs`
  Composicion de la aplicacion y middleware.
- `kdspro/src/kdspro.Api/Controllers/OrdersController.cs`
  Entrada principal del flujo operativo de pedidos.
- `kdspro/src/kdspro.Application/Services/OrderService.cs`
  Reglas de pedidos, pago, limpieza y cancelacion.
- `kdspro/src/kdspro.Application/Services/AuthService.cs`
  Autenticacion, tokens y sesion activa.
- `kdspro/src/kdspro.Infrastructure/Persistence/MongoDbContext.cs`
  Colecciones y acceso a base.

## Regla practica

- Si algo cambia el negocio, documentalo en `Application`.
- Si algo solo expone o traduce HTTP, documentalo en `Api`.
- Si aparece una validacion duplicada entre controller y servicio, conserva en controller solo la que protege contexto o autorizacion y deja la regla de negocio en el servicio.
