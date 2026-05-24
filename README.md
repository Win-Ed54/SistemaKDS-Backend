# Sistema KDS Backend

API del sistema KDS construida con .NET 8, MongoDB y SignalR.

## Capas

- `kdspro.Domain`
- `kdspro.Application`
- `kdspro.Infrastructure`
- `kdspro.Api`

## Modulos principales

- `Auth`
- `Orders`
- `Products`
- `Tables`
- `Users`
- `Waiter`
- `KdsSettings`
- `Analytics`

## Responsabilidades actuales

- Autenticacion y refresh token.
- Sesion unica por usuario.
- Reglas de negocio por rol.
- Flujo para mesa, para llevar y delivery.
- Asignacion de mesas y limpieza.
- Segmentacion en tiempo real con SignalR.
- Analitica diaria, semanal y mensual.

## Variables minimas para Railway

- `ASPNETCORE_ENVIRONMENT=Production`
- `MongoDbSettings__ConnectionString=...`
- `MongoDbSettings__DatabaseName=KDS`
- `Jwt__Key=...`
- `Jwt__Issuer=kds-api`
- `Jwt__Audience=kds-client`
- `CORS_ORIGINS=https://TU-FRONTEND.vercel.app`
- `Seed__DefaultUsers=false`

## Desarrollo local

```powershell
cd .\kdspro\src
dotnet restore
dotnet build .\kdspro.sln -m:1
dotnet run --project .\kdspro.Api
```

API local por defecto:

- `http://localhost:5162`

Hub:

- `/ordersHub`

## Endpoints clave

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /api/orders/active`
- `GET /api/orders/history`
- `POST /api/orders`
- `PATCH /api/orders/{id}/ready`
- `PATCH /api/orders/{id}/finish`
- `PATCH /api/orders/{id}/pay`
- `GET /api/waiter/summary`
- `GET /api/tables`
- `GET /api/users/staff`
- `PATCH /api/users/{id}/service-scope`
- `GET /api/analytics/daily`
- `GET /api/analytics/week`
- `GET /api/analytics/month`

## Recomendaciones de despliegue

- Ejecutar con HTTPS delante de Railway cuando aplique.
- Mantener CORS cerrado solo al frontend real.
- No exponer semillas ni credenciales de prueba.
- Confirmar que MongoDB Atlas acepte conexiones desde Railway.
- Mantener imagenes de productos en `wwwroot/images/productos` con respaldo si el entorno es efimero.

## Validacion rapida

```powershell
dotnet build .\kdspro.Api\kdspro.Api.csproj -m:1
```
