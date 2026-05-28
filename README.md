# Sistema KDS Backend

API del sistema KDS para restaurantes, construida con .NET 8, MongoDB y SignalR.

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
- `Analytics`

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

## Actualizacion 27-May

- `TakeoutDestination` acepta destinos operativos para llevar: `Mostrador`, `Autoservicio`, `Delivery` y `Mesa N`.
- Los pedidos para llevar guardan `WaiterId`, `WaiterName`, destino y direccion de delivery cuando aplica.
- El cobro soporta pagos parciales por linea mediante `ItemPayments`, `PaidQuantity`, `PaidAmount` y `RemainingAmount`.
- Los prepagos para llevar pueden pasar primero por caja y publicarse a cocina al quedar cobrados.
- La auditoria de orden conserva correlativo, creador, cobrador, cancelador, metodo de pago y timestamps relevantes.

## Flujo de pedidos para llevar

- una orden `tableNumber = 0` se registra como pedido para llevar
- conserva `WaiterId` y `WaiterName` del usuario autenticado que la creo
- conserva el destino para llevar, incluyendo `Mesa N` cuando nace desde una mesa asignada
- si el prepago para llevar esta desactivado, la orden entra a cocina de inmediato
- si el prepago para llevar esta activado, la orden aparece primero en caja
- cuando caja completa el pago, el backend publica la orden a cocina
- con prepago activo, el seguimiento en caja debe tratar esas ordenes como flujo de prepago y no como cobro normal de ordenes entregadas

## Flujo de cobro parcial

- caja puede enviar `ItemPayments` para cobrar solo cantidades seleccionadas de una orden entregada
- cada linea acumula `PaidQuantity` y expone `RemainingQuantity`
- la orden queda `IsPaid = false` mientras existan productos pendientes
- cuando todas las lineas estan pagadas, se registra `PaidAt`, `PaidByName`, metodo de pago y documento
- los totales expuestos incluyen `PaidAmount` y `RemainingAmount`

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
- `waiter`: eventos de sus propias ordenes mediante `Clients.User`, incluyendo aviso directo cuando una orden pasa a `Ready`
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

## Variables para produccion

Para produccion configura como minimo:

- `ASPNETCORE_ENVIRONMENT=Production`
- `MongoDbSettings__ConnectionString=mongodb+srv://USUARIO:CLAVE@CLUSTER.mongodb.net/?retryWrites=true&w=majority&appName=TUAPP`
- `MongoDbSettings__DatabaseName=KDS`
- `Jwt__Key=UNA_CLAVE_LARGA_Y_SECRETA_DE_32_BYTES_O_MAS`
- `Jwt__Issuer=kds-api`
- `Jwt__Audience=kds-client`
- `CORS_ORIGINS=https://TU-FRONTEND.vercel.app`
- `Seed__DefaultUsers=false`

Notas:

- `CORS_ORIGINS` acepta varios dominios separados por coma.
- En `docker-compose.yml` el backend tambien puede recibir `PUBLIC_ORIGIN`, que termina en `Cors__Origins__0`.
- Railway debe apuntar al Dockerfile de `kdspro/src/kdspro.Api`.
- MongoDB Atlas debe permitir acceso desde Railway y usar el string `mongodb+srv`.

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

En `kdspro/src/docker-compose.yml` existe soporte para levantar MongoDB, la API y el frontend.

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
- Ese frontend usa `pnpm`, asi que su `Dockerfile` instala con `pnpm install --frozen-lockfile`.

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
- El flujo de tiempo real hacia mesero depende de `WaiterId` y `ClaimTypes.NameIdentifier`, no de nombres hardcodeados por usuario.
- La documentacion evita exponer secretos, credenciales de prueba o configuracion sensible del entorno.
