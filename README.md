# Sistema KDS Backend

Backend del sistema KDS para restaurantes, construido con .NET 8, MongoDB y SignalR.

Esta API centraliza autenticacion, pedidos, stock, mesas, configuracion operativa y eventos en tiempo real para cocina, meseros, caja, host y administracion.

## Stack

- .NET 8
- ASP.NET Core Web API
- SignalR
- MongoDB
- Clean Architecture

## Estructura

- `kdspro/src/kdspro.Domain`: entidades, enums e interfaces
- `kdspro/src/kdspro.Application`: DTOs, servicios y reglas de negocio
- `kdspro/src/kdspro.Infrastructure`: persistencia MongoDB, repositorios y seed
- `kdspro/src/kdspro.Api`: controladores, hubs y configuracion HTTP

## Modulos principales

- `Auth`
- `Orders`
- `Products`
- `Tables`
- `Waiter`
- `KdsSettings`
- `Users`

## Roles soportados

- `admin`
- `cashier`
- `kitchen`
- `waiter`
- `host`

## Funcionalidad actual

- acceso por roles
- sesion unica por usuario
- pedidos en tiempo real con SignalR
- eventos segmentados por rol y por usuario cuando aplica
- control de stock y proteccion ante sobreventa
- calculo de precios desde datos reales del producto
- flujo de estados `Pending`, `Preparing`, `Ready`, `Delivered`, `Cancelled`
- cobro de ordenes
- prepago para llevar opcional
- notificacion en tiempo real a caja cuando aparece un pedido pendiente de prepago
- limpieza y liberacion de mesas
- restriccion para que el cierre de limpieza respete al mesero responsable
- configuracion dinamica del KDS
- asignacion de mesas con mesero responsable
- transferencia de mesas segun reglas operativas
- validaciones para host, mesero, caja, cocina y admin segun el flujo

## Flujo de pedidos para llevar

- una orden `tableNumber = 0` se registra como pedido para llevar
- conserva `WaiterId` y `WaiterName` del usuario autenticado que la creo
- si el prepago para llevar esta desactivado, la orden entra a cocina de inmediato
- si el prepago para llevar esta activado, la orden aparece primero en caja
- cuando caja completa el pago, el backend publica la orden a cocina

## Flujo de mesas y limpieza

- host asigna la mesa y el mesero responsable
- mesero puede seguir agregando productos a sus mesas asignadas
- cuando una mesa pagada entra en limpieza, queda bloqueada para nuevas ordenes
- al terminar limpieza, la mesa se libera y se limpia su estado operativo
- los cambios de mesa se notifican por SignalR para host, admin, caja y mesero
- admin puede intervenir para liberar mesas segun el estado real del flujo

## Hub en tiempo real

- `/ordersHub`

Eventos relevantes:

- `receiveorder`
- `orderpreparing`
- `orderready`
- `orderdelivered`
- `orderpaid`
- `ordercancelled`
- `ordercreatedforpayment`
- `stockupdated`
- `productoutofstock`
- `tablesupdated`
- `settingsupdated`
- `OrderReadyForPickup`
- `UpdateOrderStatus`

## Segmentacion de eventos

El hub distribuye eventos por grupo o por usuario:

- `kitchen`, `admin`: ordenes nuevas y cambios de estado de cocina
- `cashier`, `admin`: entregas, cobros y pendientes de prepago
- `waiter`: eventos de sus propias ordenes mediante `Clients.User`
- `waiter`, `host`, `admin`, `cashier`: cambios de estado de mesas
- `waiter`, `admin`: stock y productos agotados

## Seguridad implementada

El backend ya incorpora medidas de seguridad y endurecimiento operativo. Aqui solo se mencionan a nivel general:

- autenticacion con tokens
- autorizacion por rol
- sesion unica por usuario
- validacion de sesion activa en requests y SignalR
- segmentacion de eventos en tiempo real
- manejo de configuracion por entorno
- proteccion del flujo de autenticacion
- despliegue pensado para no exponer servicios internos innecesariamente

## Configuracion

La API usa configuracion de entorno para:

- conexion a base de datos
- JWT
- CORS
- configuracion operativa del despliegue

En `kdspro/src` existe un `.env.example` para entorno local.

## Ejecucion local

Ubicacion:

- `Sistema-KDS-Kitchen-Display-System-para-restaurantes---Backend/kdspro/src`

Comandos:

```powershell
dotnet restore
dotnet build .\kdspro.sln -m:1
dotnet run --project .\kdspro.Api
```

Acceso local por defecto:

- `http://localhost:5162`

## Docker

En `kdspro/src/docker-compose.yml` existe soporte para levantar los servicios del proyecto.

Comando recomendado:

```powershell
docker compose up -d --build
```

El frontend entra por proxy a:

- `/api`
- `/images`
- `/ordersHub`

Notas de integracion:

- El `docker-compose.yml` construye el frontend desde `Sistema-KDS-Kitchen-Display-System-para-restaurantes---Frontend`.
- Ese frontend ahora usa `pnpm`; el `Dockerfile` del frontend ya instala con `pnpm install --frozen-lockfile`, por lo que no requiere `npm ci`.

## Endpoints destacados

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `GET /api/orders/active`
- `GET /api/orders/history`
- `PATCH /api/orders/{id}/preparing`
- `PATCH /api/orders/{id}/ready`
- `PATCH /api/orders/{id}/finish`
- `PATCH /api/orders/{id}/pay`
- `PATCH /api/orders/{id}/cancel`
- `PATCH /api/orders/table/{tableNumber}/close`
- `GET /api/waiter/summary`
- `GET /api/waiter/today`
- `GET /api/tables`
- `PATCH /api/tables/{tableNumber}/seat`
- `PATCH /api/tables/{tableNumber}/start-cleaning`
- `GET /api/kdssettings`
- `PUT /api/kdssettings`

## Validacion recomendada

```powershell
dotnet build .\kdspro.Api\kdspro.Api.csproj -m:1
```

## Notas

- El backend esta pensado para mantener sincronizadas las pantallas operativas sin recarga manual.
- La documentacion evita exponer secretos, credenciales de prueba o configuracion sensible del entorno.
