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
- `RecoverPassword`: recuperacion controlada de contraseña con llave de entorno

## Roles soportados

- `admin`
- `cashier`
- `kitchen`
- `waiter`
- `host`

## Funcionalidad implementada

- Acceso por roles
- Sesion unica por usuario con invalidacion de sesiones anteriores
- Pedidos en tiempo real con SignalR
- Eventos SignalR segmentados por rol y por usuario
- Control de stock y proteccion ante sobreventa concurrente
- Calculo de precios de orden desde productos reales en base de datos
- Flujo de estados: `Pending`, `Preparing`, `Ready`, `Delivered`, `Cancelled`
- Cobro de ordenes entregadas
- Control de limpieza y liberacion de mesas
- Restriccion para que solo el mesero correspondiente pueda liberar la ultima mesa pagada que le pertenece
- Pedidos para llevar con destino operativo (`TakeoutDestination`)
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

### Segmentacion de eventos

El hub ya no usa `Clients.All` para publicar datos operativos. Los eventos se distribuyen por grupo o por usuario:

- `kitchen`, `admin`: ordenes nuevas, preparando, listas y canceladas.
- `cashier`, `admin`: entregas, cobros y cancelaciones relevantes para caja.
- `waiter`: solo recibe eventos de sus propias ordenes mediante `Clients.User(order.WaiterId)`.
- `waiter`, `host`, `admin`, `cashier`: cambios de estado de mesas.
- `waiter`, `admin`: actualizaciones de producto, stock y agotados.
- `waiter`, `host`, `admin`, `cashier`: cambios de configuracion KDS.

Esto reduce exposicion lateral: una pantalla conectada al hub no recibe datos que no necesita para operar.

## Seguridad aplicada

- JWT valida issuer, audience, lifetime y firma.
- La llave JWT es obligatoria; en produccion debe ser un secreto real de al menos 32 bytes.
- CORS en produccion usa `Cors:Origins` y no permite origenes arbitrarios.
- Docker Compose queda en `ASPNETCORE_ENVIRONMENT=Production`.
- Login y refresh usan rate limit.
- Refresh tokens nuevos se guardan como hash SHA-256.
- Mensajes de error de autenticacion son genericos.
- Se elimino el endpoint de prueba `test-mongo`.
- Se agregaron headers defensivos: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`.
- En produccion se activa HSTS.
- Los usuarios de seed solo se crean o reparan cuando `Seed:DefaultUsers=true`.
- Login normaliza usuario y rol para tolerar mayusculas/minusculas o espacios accidentales.
- Roles de JWT y SignalR se normalizan antes de autorizar o unir conexiones a grupos.
- Cada login genera un `sid` nuevo; requests autenticados y SignalR validan que la sesion del token siga siendo la activa.
- Los refresh tokens quedan ligados a la sesion activa y se revocan al iniciar una nueva sesion.
- Contraseñas antiguas en texto plano se migran a BCrypt solo despues de validar la contraseña correcta.
- Existe recuperacion de contraseña con `Auth:RecoveryKey`, desactivada si no se configura la llave.
- Los datos sensibles se esperan por variables de entorno, no en archivos versionados.

## Produccion

Variables recomendadas:

- `MongoDbSettings__ConnectionString`
- `MongoDbSettings__DatabaseName`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Cors__Origins__0`
- `Seed__DefaultUsers=false` por defecto; usar `true` solo en pruebas controladas
- `Auth__RecoveryKey` solo si se necesita recuperar contraseñas manualmente

Notas:

- `docker-compose.yml` lee `SEED_DEFAULT_USERS` y por defecto no crea/repara usuarios demo en Production.
- Antes de exponer el sistema publicamente, cambiar contraseñas demo y mantener `SEED_DEFAULT_USERS=false`.
- Usar HTTPS delante de la API y del hub SignalR.
- Configurar `PUBLIC_ORIGIN` en Docker para que coincida con el dominio real del frontend.
- Si un usuario inicia sesion en otro navegador o equipo, la sesion anterior queda invalida para evitar ordenes paralelas con el mismo operador.

## Seed de datos

El proyecto crea o repara datos iniciales cuando corresponde:

- productos demo
- mesas demo
- usuarios demo
- configuracion inicial del KDS

### Usuarios seed

Cuando `Seed__DefaultUsers=true`, el seeder:

- crea usuarios faltantes
- encuentra usuarios existentes aunque tengan mayusculas/minusculas distintas
- normaliza `Username` y `Role`
- repara la contraseña si no coincide con la contraseña seed esperada
- no borra ordenes, mesas, productos ni datos operativos

Las credenciales seed no se documentan aqui por seguridad. Si se necesitan para pruebas de produccion controlada, deben consultarse directamente en la configuracion interna del equipo o rotarse antes de exponer el sistema.

Despues de cambiar `Seed__DefaultUsers` o actualizar `DbSeeder`, se debe reconstruir/reiniciar el backend para que el seed vuelva a correr.

### Recuperacion de contraseña

Endpoint:

- `POST /api/auth/recover-password`

Requiere configurar una llave fuerte:

```env
Auth__RecoveryKey=una_llave_larga_de_32_caracteres_minimo
```

Ejemplo:

```powershell
Invoke-RestMethod -Method Post `
  -Uri "http://TU_HOST:5173/api/auth/recover-password" `
  -ContentType "application/json" `
  -Body '{"username":"usuario_a_recuperar","newPassword":"nueva_contraseña_segura","recoveryKey":"una_llave_larga_de_32_caracteres_minimo"}'
```

Si `Auth__RecoveryKey` no existe o no coincide, la recuperacion no modifica nada.

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

El compose usa variables de entorno para MongoDB, JWT, CORS y origen publico. No se deben escribir secretos reales en `appsettings.json`.

Por seguridad, el puerto directo de la API queda publicado solo en `127.0.0.1:5162`. Las pantallas deben entrar por el frontend/Nginx, que hace proxy a `/api`, `/images` y `/ordersHub`.

## Endpoints destacados

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/recover-password`
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
- Antes de produccion conviene rotar usuarios/contraseñas demo ya existentes y definir una llave JWT fuerte.
