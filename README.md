# SIGID — Sistema Inteligente de Gestión de Inventarios y Demanda

Proyecto académico para la gestión de inventarios y el análisis de la demanda de productos.

**Estado actual: Sprint 3.** Incluye la gestión del catálogo (productos, categorías y proveedores), del inventario (entradas, salidas e historial de movimientos), el acceso protegido del administrador (inicio y cierre de sesión) y la gestión de ventas (registro con actualización automática de existencias e historial), con persistencia en SQL Server mediante Entity Framework Core.

## Tecnologías

- C# / .NET 9
- ASP.NET Core MVC con Razor Views
- Entity Framework Core 9 (proveedor SQL Server)
- SQL Server (LocalDB, Express o instancia local)
- HTML, CSS y JavaScript sencillo (sin frameworks de interfaz)
- xUnit para pruebas
- Git / GitHub

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 (versión 17.12 o superior) o Visual Studio 2026 con la carga de trabajo **ASP.NET y desarrollo web**
- SQL Server o SQL Server Express LocalDB
- Git

## Arquitectura

La solución sigue una **arquitectura Onion**:

```
SIGID.Web  ──►  SIGID.Application  ──►  SIGID.Domain
    │                  ▲                    ▲
    └──► SIGID.Infrastructure ──────────────┘
         (solo para registrar la DI)
```

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `SIGID.Domain` | Entidades (`Product`, `Category`, `Supplier`, `InventoryMovement`, `Administrator`, `Sale`, `SaleDetail`) y enumerados | — |
| `SIGID.Application` | Interfaces de repositorios y servicios, DTOs, servicios y reglas de negocio | Domain |
| `SIGID.Infrastructure` | `SigidDbContext`, configuraciones EF Core, repositorios, transacciones (`EfTransactionRunner`), hash de contraseñas y migraciones | Application, Domain |
| `SIGID.Web` | Controllers, Views, ViewModels, estilos, autenticación por cookies y composición de la DI | Application, Infrastructure |

Flujo de cada operación: **View → Controller → Service → Repository → SQL Server**.
Los controllers no acceden al `DbContext` ni contienen reglas de negocio; los errores de negocio llegan como `BusinessException` y se muestran junto al campo correspondiente.

## Módulos

| Módulo | Ruta | Qué permite |
|---|---|---|
| Dashboard | `/Dashboard` | Indicadores calculados con los datos reales: productos activos, sin existencias, con stock bajo, movimientos recientes |
| Productos (HU-05) | `/Products` | Registrar, consultar y editar productos. Categoría y proveedor se eligen de los registrados |
| Categorías (HU-06) | `/Categories` | CRUD completo. No se elimina una categoría con productos |
| Proveedores (HU-07) | `/Suppliers` | CRUD completo. Identificación/RNC opcional y única. No se elimina un proveedor con productos |
| Entradas (HU-08) | `/Inventory/Entries` | Registrar y consultar entradas; suman a las existencias |
| Salidas (HU-09) | `/Inventory/Exits` | Registrar y consultar salidas; no permiten existencias negativas |
| Historial (HU-10) | `/Inventory/History` | Movimientos del más reciente al más antiguo, con filtros por producto, tipo y fechas |
| Inicio de sesión (HU-11/12) | `/Account/Login` | Acceso del administrador con usuario o correo y contraseña. Única pantalla pública |
| Registrar venta (HU-14/15) | `/Sales/Create` | Venta de uno o varios productos; descuenta existencias y genera una salida por producto |
| Historial de ventas (HU-16) | `/Sales` | Ventas de la más reciente a la más antigua, con filtros por producto y fechas, y detalle de cada venta |

Todas las pantallas, salvo el inicio de sesión y la página de error, requieren una sesión iniciada (HU-13).

### Reglas de inventario

- `MinimumStock` y `MaximumStock` son umbrales de referencia. Las existencias reales están en `Product.CurrentStock`.
- Un producto nuevo empieza con **0 existencias**. Solo cambian con entradas (suman) y salidas (restan).
- Cada entrada o salida guarda un `InventoryMovement` con la existencia resultante (`StockAfter`).
- El cambio de existencias y el movimiento se guardan en **una sola transacción**.
- La salida descuenta con un `UPDATE ... WHERE CurrentStock >= cantidad`, de modo que dos salidas simultáneas no pueden consumir las mismas unidades. Además, la base de datos tiene la restricción `CK_Products_CurrentStock` (`CurrentStock >= 0`).
- Los productos inactivos no admiten movimientos.
- Stock bajo: el producto tiene unidades, pero `CurrentStock <= MinimumStock`.

### Reglas de ventas

- Una venta tiene uno o más productos, cada uno una sola vez y con cantidad mayor que cero. Si se agrega de nuevo un producto, se suma la cantidad.
- No se venden productos inactivos ni inexistentes.
- Si un producto no tiene existencias suficientes, **se rechaza toda la venta** y se indica qué producto falla, cuánto se pidió y cuánto hay.
- La venta, sus detalles, el descuento de existencias y las salidas de inventario se guardan en **una sola transacción**: si algo falla, no se guarda nada.
- Cada producto vendido genera una salida (`InventoryMovement` de tipo `Exit`) vinculada a la venta por `SaleId`. Se reutilizan las reglas de salidas del Sprint 2 (incluido el `UPDATE ... WHERE CurrentStock >= cantidad`).
- Cada formulario de venta tiene un identificador (`RequestId`, índice único): un doble clic o un reenvío no registra la venta dos veces.
- La fecha de la venta es la del servidor al confirmar. No hay precios ni facturación (fuera del alcance).

### Seguridad del acceso

- Contraseñas guardadas solo como hash con `PasswordHasher` de ASP.NET Core Identity (PBKDF2 con sal).
- Mismo mensaje de error para usuario inexistente, contraseña incorrecta o cuenta inactiva.
- Cookie `SIGID.Auth`: `HttpOnly`, `SameSite=Lax`, solo HTTPS fuera de Development, expiración por inactividad (`Authentication:SessionMinutes`, 60 por defecto).
- La sesión se guarda en el servidor: al cerrar sesión, una copia anterior de la cookie deja de funcionar. Si la aplicación se reinicia, hay que volver a iniciar sesión.
- Política global que exige sesión (`FallbackPolicy`), más `[Authorize]` en cada controlador; los POST usan token antifalsificación.

## Cómo ejecutar

1. Abrir `SIGID.sln` en Visual Studio (los paquetes NuGet se restauran automáticamente) o desde consola:
   ```bash
   dotnet restore
   ```
2. Revisar la cadena `DefaultConnection` en `src/SIGID.Web/appsettings.json` **y** en `src/SIGID.Web/appsettings.Development.json`. Ambas deben apuntar al mismo servidor y a la misma base:
   ```
   Server=(localdb)\\mssqllocaldb;Database=SIGID;Trusted_Connection=True;...   ← LocalDB
   Server=.;Database=SIGID;Trusted_Connection=True;...                          ← instancia local por defecto
   Server=.\\SQLEXPRESS;Database=SIGID;Trusted_Connection=True;...              ← SQL Server Express
   ```
   Si se necesita usuario y contraseña, **no** los escriba en `appsettings`; use User Secrets:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena-con-credenciales>" --project src/SIGID.Web
   ```
3. Aplicar las migraciones (ver sección siguiente).
4. Configurar la cuenta inicial del administrador (ver «Cuenta inicial del administrador»).
5. Establecer `SIGID.Web` como proyecto de inicio y ejecutar (F5), o:
   ```bash
   dotnet run --project src/SIGID.Web
   ```
6. Iniciar sesión en `/Account/Login` con el usuario o el correo configurado.

## Cuenta inicial del administrador

No hay registro público ni contraseñas en el código. La primera cuenta se crea al iniciar la aplicación **solo si todavía no existe ningún administrador**, con los datos de la sección `InitialAdministrator`, que debe ir en **User Secrets** (o variables de entorno), nunca en `appsettings.json`:

```bash
dotnet user-secrets set "InitialAdministrator:UserName" "admin" --project src/SIGID.Web
dotnet user-secrets set "InitialAdministrator:Email" "admin@sigid.local" --project src/SIGID.Web
dotnet user-secrets set "InitialAdministrator:Password" "<una-contraseña-de-al-menos-8-caracteres>" --project src/SIGID.Web
```

En Visual Studio también se puede usar clic derecho en `SIGID.Web` → **Administrar secretos de usuario**:

```json
{
  "InitialAdministrator": {
    "UserName": "admin",
    "Email": "admin@sigid.local",
    "Password": "<contraseña>"
  }
}
```

Variables de entorno equivalentes: `InitialAdministrator__UserName`, `InitialAdministrator__Email`, `InitialAdministrator__Password`.

- Se guarda solo el hash; la contraseña no aparece en la base de datos ni en los registros.
- Ejecutar la aplicación varias veces no crea cuentas duplicadas.
- Una vez creada la cuenta, conviene borrar la contraseña de los secretos: `dotnet user-secrets remove "InitialAdministrator:Password" --project src/SIGID.Web`.
- En la consola de la aplicación aparece un aviso si falta la configuración o si los datos no son válidos (contraseña de menos de 8 caracteres, correo inválido).

## Migraciones (Entity Framework Core)

Las migraciones están en `src/SIGID.Infrastructure/Persistence/Migrations`:

| Migración | Contenido |
|---|---|
| `InitialCreate` (Sprint 1) | Tablas `Categories`, `Suppliers` y `Products` |
| `Sprint2Inventory` (Sprint 2) | Columna `Products.CurrentStock` (0 para los productos existentes) y su restricción `>= 0`; `Suppliers.Identification` pasa a ser opcional con índice único filtrado; nueva tabla `InventoryMovements` |
| `Sprint3SalesAuth` (Sprint 3) | Nuevas tablas `Administrators`, `Sales` y `SaleDetails`; columna opcional `InventoryMovements.SaleId` con su clave foránea |

Las migraciones **no borran datos**: lo registrado en sprints anteriores se conserva. No use `Drop-Database` ni borre el historial de migraciones; basta con `Update-Database` sobre la base existente.

**Consola del Administrador de paquetes (Visual Studio):**

```powershell
Update-Database -Project SIGID.Infrastructure -StartupProject SIGID.Web
```

**CLI de .NET** (requiere `dotnet tool install --global dotnet-ef --version 9.*`):

```bash
dotnet ef database update --project src/SIGID.Infrastructure --startup-project src/SIGID.Web
```

Para comprobar que las migraciones están al día con el modelo (EF Core 8 o superior):

```bash
dotnet ef migrations has-pending-model-changes --project src/SIGID.Infrastructure --startup-project src/SIGID.Web
```

Para futuros cambios del modelo:

```powershell
Add-Migration NombreDelCambio -Project SIGID.Infrastructure -StartupProject SIGID.Web -OutputDir Persistence/Migrations
```

## Pruebas

- `SIGID.Domain.Tests`: regla de estado de existencias del producto.
- `SIGID.Application.Tests`: reglas de negocio de productos, categorías, proveedores, inventario, dashboard, autenticación y ventas. Usan repositorios en memoria (`Fakes/`), sin necesidad de SQL Server.

```bash
dotnet test
```

En Visual Studio: **Prueba → Ejecutar todas las pruebas**.

## Comprobación manual del Sprint 2

1. Crear una categoría y un proveedor; intentar repetir el nombre de la categoría o el RNC del proveedor.
2. Registrar un producto (empieza con 0 existencias); probar SKU repetido, sin categoría o con máximo menor que mínimo.
3. Registrar una entrada y comprobar que las existencias suben.
4. Registrar una salida mayor a las existencias (debe rechazarse) y otra válida.
5. Consultar el historial y aplicar filtros por producto, tipo y fechas.
6. Intentar eliminar la categoría o el proveedor del producto (debe bloquearse).
7. Revisar las tablas en SQL Server: `Products.CurrentStock` y `InventoryMovements`.

## Comprobación manual del Sprint 3

1. Sin iniciar sesión, abrir `/Products` o `/Sales`: debe aparecer el inicio de sesión.
2. Probar usuario o contraseña incorrectos (mismo mensaje en ambos casos, la contraseña no se vuelve a mostrar) y luego iniciar sesión correctamente: se abre el dashboard.
3. **Ventas → Registrar venta**: agregar dos productos, volver a agregar uno de ellos (se suma la cantidad), quitar uno y confirmar.
4. Intentar vender más de lo disponible: la venta completa se rechaza y las existencias no cambian.
5. Verificar en **Productos** y en **Inventario → Historial** que bajaron las existencias y que hay una salida «Venta #N» por producto.
6. **Ventas → Historial de ventas**: filtrar por producto y por fechas, abrir el detalle y volver.
7. **Cerrar sesión**: vuelve al inicio de sesión y las pantallas dejan de estar disponibles.
8. En SQL Server, revisar `Administrators` (solo hash), `Sales`, `SaleDetails` e `InventoryMovements.SaleId`.

## Flujo de trabajo con Git

- `main`: versión estable / entregas.
- `develop`: integración del sprint en curso.
- `feature/<nombre>`: una rama por historia o tarea (por ejemplo `feature/hu-08-entradas`), creada desde `develop` y fusionada mediante Pull Request.

No se deben subir `bin/`, `obj/`, `.vs/` ni archivos con credenciales (ya excluidos en `.gitignore`).
