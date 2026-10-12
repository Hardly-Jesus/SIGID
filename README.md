# SIGID — Sistema Inteligente de Gestión de Inventarios y Demanda

Proyecto académico para la gestión de inventarios y el análisis de la demanda de productos.

**Estado actual: Sprint 2.** Incluye la gestión completa del catálogo (productos, categorías y proveedores) y del inventario (entradas, salidas e historial de movimientos), con persistencia en SQL Server mediante Entity Framework Core.

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
| `SIGID.Domain` | Entidades (`Product`, `Category`, `Supplier`, `InventoryMovement`) y enumerados | — |
| `SIGID.Application` | Interfaces de repositorios y servicios, DTOs, servicios y reglas de negocio | Domain |
| `SIGID.Infrastructure` | `SigidDbContext`, configuraciones EF Core, repositorios y migraciones | Application, Domain |
| `SIGID.Web` | Controllers, Views, ViewModels, estilos y composición de la DI | Application, Infrastructure |

Flujo de cada operación: **View → Controller → Service → Repository → SQL Server**.
Los controllers no acceden al `DbContext` ni contienen reglas de negocio; los errores de negocio llegan como `BusinessException` y se muestran junto al campo correspondiente.

## Módulos del Sprint 2

| Módulo | Ruta | Qué permite |
|---|---|---|
| Dashboard | `/Dashboard` | Indicadores calculados con los datos reales: productos activos, sin existencias, con stock bajo, movimientos recientes |
| Productos (HU-05) | `/Products` | Registrar, consultar y editar productos. Categoría y proveedor se eligen de los registrados |
| Categorías (HU-06) | `/Categories` | CRUD completo. No se elimina una categoría con productos |
| Proveedores (HU-07) | `/Suppliers` | CRUD completo. Identificación/RNC opcional y única. No se elimina un proveedor con productos |
| Entradas (HU-08) | `/Inventory/Entries` | Registrar y consultar entradas; suman a las existencias |
| Salidas (HU-09) | `/Inventory/Exits` | Registrar y consultar salidas; no permiten existencias negativas |
| Historial (HU-10) | `/Inventory/History` | Movimientos del más reciente al más antiguo, con filtros por producto, tipo y fechas |

### Reglas de inventario

- `MinimumStock` y `MaximumStock` son umbrales de referencia. Las existencias reales están en `Product.CurrentStock`.
- Un producto nuevo empieza con **0 existencias**. Solo cambian con entradas (suman) y salidas (restan).
- Cada entrada o salida guarda un `InventoryMovement` con la existencia resultante (`StockAfter`).
- El cambio de existencias y el movimiento se guardan en **una sola transacción**.
- La salida descuenta con un `UPDATE ... WHERE CurrentStock >= cantidad`, de modo que dos salidas simultáneas no pueden consumir las mismas unidades. Además, la base de datos tiene la restricción `CK_Products_CurrentStock` (`CurrentStock >= 0`).
- Los productos inactivos no admiten movimientos.
- Stock bajo: el producto tiene unidades, pero `CurrentStock <= MinimumStock`.

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
4. Establecer `SIGID.Web` como proyecto de inicio y ejecutar (F5), o:
   ```bash
   dotnet run --project src/SIGID.Web
   ```

## Migraciones (Entity Framework Core)

Las migraciones están en `src/SIGID.Infrastructure/Persistence/Migrations`:

| Migración | Contenido |
|---|---|
| `InitialCreate` (Sprint 1) | Tablas `Categories`, `Suppliers` y `Products` |
| `Sprint2Inventory` (Sprint 2) | Columna `Products.CurrentStock` (0 para los productos existentes) y su restricción `>= 0`; `Suppliers.Identification` pasa a ser opcional con índice único filtrado; nueva tabla `InventoryMovements` |

La migración del Sprint 2 **no borra datos**: los productos, categorías y proveedores existentes se conservan.

**Consola del Administrador de paquetes (Visual Studio):**

```powershell
Update-Database -Project SIGID.Infrastructure -StartupProject SIGID.Web
```

**CLI de .NET** (requiere `dotnet tool install --global dotnet-ef --version 9.*`):

```bash
dotnet ef database update --project src/SIGID.Infrastructure --startup-project src/SIGID.Web
```

Para futuros cambios del modelo:

```powershell
Add-Migration NombreDelCambio -Project SIGID.Infrastructure -StartupProject SIGID.Web -OutputDir Persistence/Migrations
```

## Pruebas

- `SIGID.Domain.Tests`: regla de estado de existencias del producto.
- `SIGID.Application.Tests`: reglas de negocio de productos, categorías, proveedores, inventario y dashboard. Usan repositorios en memoria (`Fakes/`), sin necesidad de SQL Server.

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

## Flujo de trabajo con Git

- `main`: versión estable / entregas.
- `develop`: integración del sprint en curso.
- `feature/<nombre>`: una rama por historia o tarea (por ejemplo `feature/hu-08-entradas`), creada desde `develop` y fusionada mediante Pull Request.

No se deben subir `bin/`, `obj/`, `.vs/` ni archivos con credenciales (ya excluidos en `.gitignore`).
