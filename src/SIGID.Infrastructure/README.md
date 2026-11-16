# SIGID.Infrastructure

Capa de infraestructura: acceso a SQL Server con EF Core, repositorios, migraciones, hash de contraseñas y el motor de predicción SSA de ML.NET.

Inventario de dependencias: por carpeta y clase, los espacios de nombres que importa con `using`, los paquetes NuGet y su uso. Las marcadas con **(S4)** se agregaron o cambiaron en el Sprint 4 (EP-05) y las marcadas con **(Opt)**, en la optimización de la EP-05.

## Paquetes y referencias

Referencias de proyecto: `SIGID.Application`, `SIGID.Domain`.

| Paquete NuGet | Versión | Uso |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | 9.0.0 | DbContext, consultas LINQ y migraciones. |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.0 | Proveedor de SQL Server (`UseSqlServer`). |
| `Microsoft.Extensions.Configuration.Abstractions` | 9.0.0 | Lectura de la cadena de conexión en `DependencyInjection`. |
| `Microsoft.Extensions.Identity.Core` | 9.0.0 | `PasswordHasher` para el hash de contraseñas (Sprint 3). |
| `Microsoft.ML` | 4.0.3 | `MLContext` y carga de datos para el modelo predictivo (Sprint 4). |
| `Microsoft.ML.TimeSeries` | 4.0.3 | `ForecastBySsa` y `TimeSeriesPredictionEngine`: algoritmo SSA (Sprint 4). |

## (raíz)

| Clase | `using` | Uso |
|---|---|---|
| `DependencyInjection` **(S4)** | `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`, `SIGID.Application.Interfaces.Forecasting`, `SIGID.Application.Interfaces.Persistence`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Security`, `SIGID.Infrastructure.Forecasting`, `SIGID.Infrastructure.Persistence`, `SIGID.Infrastructure.Persistence.Repositories`, `SIGID.Infrastructure.Security` | Registra el DbContext (SQL Server), los repositorios y el motor SSA en el contenedor de dependencias. |

## Forecasting

| Clase | `using` | Uso |
|---|---|---|
| `SsaDemandForecastEngine` **(S4)** **(Opt)** | `Microsoft.ML`, `Microsoft.ML.Transforms.TimeSeries`, `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand`, `SIGID.Application.Interfaces.Forecasting` | Predicción de demanda con ML.NET y el algoritmo Singular Spectrum Analysis (SSA). Recibe una serie diaria ya preparada (ordenada, sin huecos) y devuelve el pronóstico. |

## Persistence/Configurations

| Clase | `using` | Uso |
|---|---|---|
| `AdministratorConfiguration` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad Administrator (columnas, índices y restricciones). |
| `CategoryConfiguration` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad Category (columnas, índices y restricciones). |
| `InventoryMovementConfiguration` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad InventoryMovement (columnas, índices y restricciones). |
| `ProductConfiguration` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad Product (columnas, índices y restricciones). |
| `SaleConfiguration` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad Sale (columnas, índices y restricciones). |
| `SaleDetailConfiguration` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad SaleDetail (columnas, índices y restricciones). |
| `SupplierConfiguration` | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad Supplier (columnas, índices y restricciones). |
| `SyntheticSalesBatchConfiguration` **(S4)** | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad SyntheticSalesBatch (columnas, índices y restricciones). |
| `SyntheticSalesRecordConfiguration` **(S4)** | `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Metadata.Builders`, `SIGID.Domain.Entities` | Configuración de EF Core de la entidad SyntheticSalesRecord (columnas, índices y restricciones). |

## Persistence

| Clase | `using` | Uso |
|---|---|---|
| `EfTransactionRunner` | `SIGID.Application.Interfaces.Persistence` | Ejecuta una operación dentro de una transacción de SQL Server. Si la operación falla, se revierte todo lo que hizo. |
| `PersistenceErrors` | `Microsoft.EntityFrameworkCore`, `SIGID.Application.Exceptions` | Guarda los cambios y convierte los errores de base de datos en mensajes comprensibles. Las reglas se validan antes en Application; esto cubre casos poco comunes, como dos usuarios guardando el mismo dato al mismo tiempo. |
| `SigidDbContext` **(S4)** | `Microsoft.EntityFrameworkCore`, `SIGID.Domain.Entities` | DbContext de EF Core: tablas de SIGID, incluidas las de datos de prueba del Sprint 4. |

## Persistence/Repositories

| Clase | `using` | Uso |
|---|---|---|
| `AdministratorRepository` | `Microsoft.EntityFrameworkCore`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Domain.Entities` | Implementación con EF Core de IAdministratorRepository. |
| `CategoryRepository` | `Microsoft.EntityFrameworkCore`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Domain.Entities` | Implementación con EF Core de ICategoryRepository. |
| `InventoryMovementRepository` **(Opt)** | `Microsoft.EntityFrameworkCore`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Domain.Entities`, `SIGID.Domain.Enums` | Implementación con EF Core de IInventoryMovementRepository. |
| `ProductRepository` | `Microsoft.EntityFrameworkCore`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Domain.Entities` | Implementación con EF Core de IProductRepository. |
| `SaleRepository` **(S4)** **(Opt)** | `Microsoft.EntityFrameworkCore`, `SIGID.Application.DTOs.Sales`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Domain.Entities` | Implementación con EF Core de ISaleRepository. |
| `SupplierRepository` | `Microsoft.EntityFrameworkCore`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Domain.Entities` | Implementación con EF Core de ISupplierRepository. |
| `SyntheticSalesRepository` **(S4)** | `Microsoft.EntityFrameworkCore`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Domain.Entities` | Implementación con EF Core de ISyntheticSalesRepository. |

## Security

| Clase | `using` | Uso |
|---|---|---|
| `PasswordHashingService` | `Microsoft.AspNetCore.Identity`, `SIGID.Application.Interfaces.Security`, `SIGID.Domain.Entities` | Usa el PasswordHasher oficial de ASP.NET Core Identity (PBKDF2 con sal aleatoria). |

## Persistence/Migrations

Migraciones generadas por EF Core (`using Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Migrations`, `Microsoft.EntityFrameworkCore.Infrastructure`). No se modificaron las anteriores.

- `20261001173638_InitialCreate.cs`
- `20261008150000_Sprint2Inventory.cs`
- `20261020150000_Sprint3SalesAuth.cs`
- `20261106193543_Sprint4DemandForecast.cs` **(S4)**
- `SigidDbContextModelSnapshot.cs` **(S4)**
