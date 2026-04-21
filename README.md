# Sistema KDS Backend

Backend del sistema KDS para restaurantes, construido con .NET 8, MongoDB y SignalR. Esta API centraliza acceso por roles, pedidos, stock, mesas, configuracion operativa y eventos en tiempo real para cocina, meseros, caja, host y administracion.

## Stack

- .NET 8
- ASP.NET Core Web API
- SignalR
- MongoDB
- autenticacion por tokens
- Clean Architecture

## Estructura

- `kdspro/src/kdspro.Domain`: entidades, enums e interfaces
- `kdspro/src/kdspro.Application`: DTOs, servicios y reglas de negocio
- `kdspro/src/kdspro.Infrastructure`: persistencia MongoDB, repositorios y seeding
- `kdspro/src/kdspro.Api`: controladores, autenticacion, hubs, configuracion HTTP

## Modulos principales

- `Auth`: acceso y renovacion de sesion
- `Orders`: creacion de pedidos, estados, cobro, cancelacion y cierre de mesa
- `Products`: catalogo y stock
- `Tables`: asignacion de mesas, limpieza y estado operativo
- `Waiter`: resumen y actividad diaria del mesero autenticado
- `KdsSettings`: configuracion dinamica de limites operativos
- `Users`: consultas auxiliares de usuarios por rol

## Roles soportados

- `admin`
- `cashier`
- `kitchen`
- `waiter`
- `host`

## Funcionalidad implementada

- Acceso por roles
- Pedidos en tiempo real con SignalR
- Control de stock y proteccion ante sobreventa concurrente
- Flujo de estados: `Pending`, `Preparing`, `Ready`, `Delivered`, `Cancelled`
- Cobro de ordenes entregadas
- Control de limpieza y liberacion de mesas
- Restriccion para que solo el mesero correspondiente pueda liberar la ultima mesa pagada que le pertenece
- Configuracion KDS desde base de datos:
  - modo `quick-service`
  - modo `restaurant`
  - maximo de productos distintos
  - maximo de unidades totales
  - maximo por producto
  - umbral de alerta para orden grande

## Hub en tiempo real

- `/ordersHub`

Eventos relevantes:

- `receiveorder`
- `orderpreparing`
- `orderready`
- `orderdelivered`
- `orderpaid`
- `ordercancelled`
- `stockupdated`
- `productoutofstock`
- `tablesupdated`
- `settingsupdated`
- `OrderReadyForPickup`
- `UpdateOrderStatus`

## Seed de datos

El proyecto crea datos iniciales si la base esta vacia:

- productos demo
- mesas demo
- usuarios demo
- configuracion inicial del KDS

Las cuentas de prueba del entorno local fueron omitidas de este documento por seguridad. Si se necesita trabajar con usuarios demo, deben configurarse localmente fuera del repositorio publico.

## Configuracion

La API usa configuracion del entorno desde `appsettings` o variables equivalentes.

Valores esperados por la aplicacion:

- parametros de base de datos
- identificadores del entorno
- secretos de firma o acceso

En `kdspro/src` tambien existe un `.env.example` para el entorno local del proyecto.

## Ejecucion local

Ubicacion de trabajo:

- `Sistema-KDS-Kitchen-Display-System-para-restaurantes---Backend/kdspro/src`

Comandos:

```powershell
dotnet restore
dotnet build .\kdspro.sln -m:1
dotnet run --project .\kdspro.Api
```

Por defecto la API queda disponible en:

- `http://localhost:5162`

## Docker

En `kdspro/src/docker-compose.yml` existe soporte para levantar servicios del proyecto en contenedores.

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

- El build actual compila correctamente.
- Puede aparecer una advertencia nullable relacionada con configuracion del entorno en `Program.cs`; no bloquea la compilacion.
- Antes de produccion conviene reemplazar datos de prueba, restringir origenes y administrar secretos fuera del repositorio.
