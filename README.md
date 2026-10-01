# SIGID — Sistema Inteligente de Gestión de Inventarios y Demanda

Proyecto académico para la gestión de inventarios y el análisis de la demanda de productos.

Este repositorio contiene la **estructura inicial** de la solución (Sprint 1): arquitectura Onion, persistencia con Entity Framework Core sobre SQL Server, entidades base (Producto, Categoría, Proveedor) y las pantallas iniciales del módulo de productos.

## Tecnologías

- C# / .NET 9
- ASP.NET Core MVC con Razor Views
- Entity Framework Core 9 (proveedor SQL Server)
- SQL Server (LocalDB en desarrollo)
- Bootstrap 5 (incluido localmente en `wwwroot/lib`)
- xUnit (proyectos de pruebas, aún sin pruebas)
- Git / GitHub

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022 (versión 17.12 o superior) o Visual Studio 2026 con la carga de trabajo **ASP.NET y desarrollo web**
- SQL Server o SQL Server Express LocalDB (se instala con Visual Studio)
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
| `SIGID.Domain` | Entidades del negocio (`Product`, `Category`, `Supplier`) | — |
| `SIGID.Application` | Interfaces de repositorios y servicios, DTOs y lógica de negocio | Domain |
| `SIGID.Infrastructure` | `SigidDbContext`, configuraciones EF Core, repositorios, migraciones | Application, Domain |
| `SIGID.Web` | Controllers, Views, ViewModels, configuración y composición de la DI | Application, Infrastructure |

Flujo de una operación: **View → Controller → Service → Repository → SQL Server**.
Los controllers nunca acceden directamente al `DbContext`.

## Estructura

```
SIGID/
├── SIGID.sln
├── src/
│   ├── SIGID.Domain/           Entities/, Enums/
│   ├── SIGID.Application/      Interfaces/, Services/, DTOs/
│   ├── SIGID.Infrastructure/   Persistence/ (DbContext, Configurations, Repositories, Migrations), DependencyInjection.cs
│   └── SIGID.Web/              Controllers/, Views/, ViewModels/, wwwroot/
└── tests/
    ├── SIGID.Domain.Tests/
    └── SIGID.Application.Tests/
```

## Cómo ejecutar

1. Clonar el repositorio:
   ```bash
   git clone <url-del-repositorio>
   cd SIGID
   ```
2. Abrir `SIGID.sln` en Visual Studio (los paquetes NuGet se restauran automáticamente) o restaurar desde consola:
   ```bash
   dotnet restore
   ```
3. Revisar la cadena de conexión `DefaultConnection` en `src/SIGID.Web/appsettings.Development.json`.
   Por defecto usa LocalDB con autenticación de Windows (sin credenciales en el código):
   ```
   Server=(localdb)\mssqllocaldb;Database=SIGID_Dev;Trusted_Connection=True;...
   ```
   Si se necesita usuario y contraseña, **no** los escriba en `appsettings`; use User Secrets:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<cadena-con-credenciales>" --project src/SIGID.Web
   ```
4. Crear la migración inicial y la base de datos (ver sección siguiente).
5. Establecer `SIGID.Web` como proyecto de inicio y ejecutar (F5), o:
   ```bash
   dotnet run --project src/SIGID.Web
   ```

> La aplicación arranca aunque la base de datos todavía no exista; la pantalla de productos mostrará un aviso hasta que se apliquen las migraciones.

## Migraciones (Entity Framework Core)

Las migraciones se guardan en `src/SIGID.Infrastructure/Persistence/Migrations`.

**Consola del Administrador de paquetes (Visual Studio)** — proyecto predeterminado: `SIGID.Infrastructure`, proyecto de inicio: `SIGID.Web`:

```powershell
Add-Migration InitialCreate -OutputDir Persistence/Migrations
Update-Database
```

**CLI de .NET** (requiere `dotnet tool install --global dotnet-ef --version 9.*`):

```bash
dotnet ef migrations add InitialCreate --project src/SIGID.Infrastructure --startup-project src/SIGID.Web --output-dir Persistence/Migrations
dotnet ef database update --project src/SIGID.Infrastructure --startup-project src/SIGID.Web
```

La migración inicial crea las tablas `Categories`, `Suppliers` y `Products` con:

- Claves primarias en las tres tablas.
- Claves foráneas `Products.CategoryId → Categories.Id` y `Products.SupplierId → Suppliers.Id` (borrado restringido).
- Índices únicos en `Products.SKU`, `Categories.Name` y `Suppliers.Identification`.
- Restricciones de stock: `MinimumStock >= 0` y `MaximumStock >= MinimumStock`.

## Pruebas

Los proyectos `SIGID.Domain.Tests` y `SIGID.Application.Tests` están configurados con xUnit, pero todavía no contienen pruebas.

```bash
dotnet test
```

## Flujo de trabajo con Git

- `main`: versión estable / entregas.
- `develop`: integración del sprint en curso.
- `feature/<nombre>`: una rama por historia o tarea (por ejemplo `feature/hu-05-registro-productos`), creada desde `develop` y fusionada mediante Pull Request.

No se deben subir `bin/`, `obj/`, `.vs/` ni archivos con credenciales (ya excluidos en `.gitignore`).

## Estado del Sprint 1

| Historia | Estado |
|---|---|
| HU-01 Repositorio y control de versiones | Archivos base listos (`.gitignore`, `.editorconfig`, `README.md`) |
| HU-02 Solución y arquitectura Onion | Estructura y referencias entre capas configuradas |
| HU-03 Base de datos y persistencia | EF Core + SQL Server + `SigidDbContext` configurados |
| HU-04 Entidades iniciales | `Product`, `Category`, `Supplier` y sus relaciones configuradas |
| HU-05 Registrar y consultar productos | Estructura base (repositorio, servicio, controller y vistas); pendiente cargar categorías y proveedores en el formulario y validar el flujo completo |
