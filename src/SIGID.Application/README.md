# SIGID.Application

Capa de aplicación: DTOs, contratos (interfaces) y servicios con las reglas del negocio. Depende solo de SIGID.Domain.

Inventario de dependencias: por carpeta y clase, los espacios de nombres que importa con `using`, los paquetes NuGet y su uso. Las marcadas con **(S4)** se agregaron o cambiaron en el Sprint 4 (EP-05) y las marcadas con **(Opt)**, en la optimización de la EP-05.

## Paquetes y referencias

No usa paquetes NuGet. Solo el SDK `Microsoft.NET.Sdk` (net9.0). Referencia de proyecto: `SIGID.Domain`. Expone sus clases internas a `SIGID.Application.Tests` con `InternalsVisibleTo`.

## Configuration

| Clase | `using` | Uso |
|---|---|---|
| `DemandForecastOptions` **(S4)** **(Opt)** | — | Parámetros del análisis y de la predicción de demanda (sección "DemandForecast"). Están todos aquí para no repartir números sueltos por el código. Ver docs/EP-05_Modelo_SSA.md y docs/EP-05_Optimizacion.md para la explicación de cada uno. |
| `SyntheticDataOptions` **(S4)** | — | Configuración del generador de datos históricos de prueba (sección "SyntheticData"). |

## DTOs/Authentication

| Clase | `using` | Uso |
|---|---|---|
| `AuthenticationResultDto` | — | Resultado de la autenticación. Nunca incluye la contraseña ni el hash. |
| `InitialAdministratorDto` | — | Datos de la cuenta inicial del administrador. Se leen de la configuración externa (secretos de desarrollo o variables de entorno), nunca del código fuente. |
| `InitialAdministratorResult` | — | Resultado de crear la cuenta inicial del administrador. |
| `LoginDto` | — | Credenciales que envía el administrador para iniciar sesión. |

## DTOs

| Clase | `using` | Uso |
|---|---|---|
| `CategoryDto` | — | Datos de una categoría para las pantallas y servicios. |
| `DashboardSummaryDto` | — | Indicadores del dashboard calculados a partir de los datos reales. |
| `InventoryMovementDto` | `SIGID.Domain.Enums` | Movimiento de inventario listo para mostrarse en listados e historial. |
| `MovementFilterDto` | `SIGID.Domain.Enums` | Filtros del historial de movimientos. Los valores nulos no filtran. |
| `PagedResultDto` **(Opt)** | — | Una página de resultados de un listado grande (historiales). |
| `ProductDto` | `SIGID.Domain.Enums` | Objeto de transferencia de datos de producto entre la capa Web y Application. |
| `RegisterMovementDto` | — | Datos para registrar una entrada o una salida de inventario. El tipo de movimiento lo define el método del servicio que se use. |
| `SupplierDto` | — | Datos de un proveedor para las pantallas y servicios. |

## DTOs/Demand

| Clase | `using` | Uso |
|---|---|---|
| `DemandDataAvailabilityDto` **(S4)** **(Opt)** | — | Estado del historial de un producto frente al modelo predictivo (HU-21). |
| `DemandEvaluationDtos` **(S4)** **(Opt)** | — | Resultado de comparar SSA con el modelo de referencia, según el MAE del período evaluado. |
| `DemandForecastDtos` **(S4)** **(Opt)** | — | Solicitud de predicción (HU-19 / HU-20). |
| `DemandProfileDto` **(Opt)** | — | Comportamiento de las ventas de un producto en el historial de entrenamiento. |
| `DemandSeriesDto` **(S4)** | — | Cantidad vendida de un producto en un día. |
| `HistoricalSalesGenerationDto` **(S4)** | `SIGID.Domain.Enums` | Parámetros para generar datos históricos de prueba (HU-17). |
| `SalesAnalysisDtos` **(S4)** | — | Filtros del análisis histórico de ventas (HU-18). |
| `SalesDataSource` **(S4)** | — | Origen de los datos históricos. Nunca se mezclan: cada consulta usa uno solo. |
| `TrainingPeriod` **(Opt)** | — | Historial que se usa para entrenar el modelo. Los períodos relativos se cuentan hacia atrás desde el último día de entrenamiento permitido (ayer en la predicción; el día anterior al período evaluado en la evaluación). |

## DTOs/Sales

| Clase | `using` | Uso |
|---|---|---|
| `RegisterSaleDto` | — | Datos para registrar una venta. |
| `SaleDto` | — | Venta registrada con sus productos. |
| `SaleHistoryDto` | — | Fila del historial de ventas. |
| `SaleRegistrationResultDto` | — | Resultado del registro de una venta: la venta guardada o los errores que la impidieron. |

## Exceptions

| Clase | `using` | Uso |
|---|---|---|
| `BusinessException` | — | Error de regla de negocio. El mensaje se muestra tal cual al usuario. PropertyName indica el campo del formulario relacionado (si aplica). |

## Interfaces/Forecasting

| Clase | `using` | Uso |
|---|---|---|
| `IDemandForecastEngine` **(S4)** | `SIGID.Application.DTOs.Demand` | Motor de predicción de series temporales. La implementación con ML.NET (SSA) está en Infrastructure, así Application no depende de ML.NET. |

## Interfaces/Persistence

| Clase | `using` | Uso |
|---|---|---|
| `ITransactionRunner` | — | Ejecuta varias operaciones de base de datos como una sola transacción. Si la operación termina bien se confirma; si lanza una excepción se revierte todo. La implementación con Entity Framework está en Infrastructure. |

## Interfaces/Repositories

| Clase | `using` | Uso |
|---|---|---|
| `IAdministratorRepository` | `SIGID.Domain.Entities` | Contrato de acceso a los administradores. |
| `ICategoryRepository` | `SIGID.Domain.Entities` | Contrato de acceso a las categorías. |
| `IInventoryMovementRepository` **(Opt)** | `SIGID.Domain.Entities`, `SIGID.Domain.Enums` | Contrato de acceso a los movimientos de inventario (con historial por páginas y totales calculados en la base de datos). |
| `IProductRepository` | `SIGID.Domain.Entities` | Contrato de acceso a los productos. |
| `ISaleRepository` **(S4)** **(Opt)** | `SIGID.Application.DTOs.Sales`, `SIGID.Domain.Entities` | Contrato de acceso a las ventas. En el Sprint 4 se agregaron las cantidades diarias por producto y la fecha de la primera venta; en la optimización, la primera venta de un producto y el historial por páginas. |
| `ISupplierRepository` | `SIGID.Domain.Entities` | Contrato de acceso a los proveedores. |
| `ISyntheticSalesRepository` **(S4)** | `SIGID.Domain.Entities` | Implementación con EF Core de IISyntheticSalesRepository. |

## Interfaces/Security

| Clase | `using` | Uso |
|---|---|---|
| `IPasswordHashingService` | — | Genera y verifica hashes de contraseñas. Se implementa con PasswordHasher de ASP.NET Core. |

## Interfaces/Services

| Clase | `using` | Uso |
|---|---|---|
| `IAuthenticationService` | `SIGID.Application.DTOs.Authentication` | Contrato del inicio de sesión del administrador. |
| `ICategoryService` | `SIGID.Application.DTOs` | Contrato del servicio de categorías. |
| `IDashboardService` | `SIGID.Application.DTOs` | Contrato del resumen del dashboard. |
| `IDemandForecastEvaluationService` **(S4)** | `SIGID.Application.DTOs.Demand` | Evaluación de la precisión del modelo predictivo (HU-21). |
| `IDemandForecastService` **(S4)** | `SIGID.Application.DTOs.Demand` | Predicción de demanda por producto con ML.NET/SSA (HU-19, HU-20). |
| `IDemandHistoryService` **(S4)** | `SIGID.Application.DTOs.Demand` | Prepara la serie diaria de ventas de un producto (real o de prueba) para el análisis y el modelo. |
| `IInventoryService` **(Opt)** | `SIGID.Domain.Enums`, `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Sales` | Contrato del servicio de entradas, salidas e historial (con versión por páginas). |
| `IProductService` | `SIGID.Application.DTOs` | Contrato del servicio de productos. |
| `ISaleService` **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Sales` | Contrato del registro e historial de ventas (con versión por páginas). |
| `ISalesAnalysisService` **(S4)** | `SIGID.Application.DTOs.Demand` | Análisis histórico de ventas por producto (HU-18). |
| `ISupplierService` | `SIGID.Application.DTOs` | Contrato del servicio de proveedores. |
| `ISyntheticSalesService` **(S4)** | `SIGID.Application.DTOs.Demand` | Generación y consulta de datos históricos de prueba (HU-17). |

## Services/Authentication

| Clase | `using` | Uso |
|---|---|---|
| `AuthenticationService` | `System.Text.RegularExpressions`, `SIGID.Application.DTOs.Authentication`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Security`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Entities` | Autenticación del administrador (HU-11). |

## Services/Categories

| Clase | `using` | Uso |
|---|---|---|
| `CategoryService` | `SIGID.Application.DTOs`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Entities` | Lógica de negocio de categorías (HU-06). |

## Services/Dashboard

| Clase | `using` | Uso |
|---|---|---|
| `DashboardService` | `SIGID.Application.DTOs`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services`, `SIGID.Application.Services.Inventory`, `SIGID.Application.Services.Products`, `SIGID.Domain.Enums` | Calcula los indicadores del dashboard a partir de los datos almacenados. |

## Services/Demand

| Clase | `using` | Uso |
|---|---|---|
| `ChronologicalSplit` **(S4)** | `SIGID.Application.DTOs.Demand` | Divide una serie diaria en entrenamiento y evaluación respetando el orden del tiempo (HU-21). Los últimos días se reservan para evaluar; nunca se usan particiones aleatorias. |
| `DemandDataRules` **(S4)** **(Opt)** | `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand` | Reglas para decidir si el historial de un producto permite pronosticar y evaluar (HU-19 / HU-21). |
| `DemandForecastEvaluationService` **(S4)** **(Opt)** | `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Forecasting`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services` | Evalúa la precisión del modelo SSA con datos conocidos (HU-21). Reserva los últimos días del historial, entrena solo con los anteriores, pronostica los días reservados y compara con lo que realmente pasó y con un modelo de referencia. |
| `DemandForecastService` **(S4)** **(Opt)** | `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Forecasting`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services` | Coordina la predicción de demanda (HU-19): obtiene el historial, valida que alcance, entrena el modelo SSA (a través de IDemandForecastEngine) y prepara los resultados. No modifica ventas, existencias ni movimientos. |
| `DemandHistoryService` **(S4)** **(Opt)** | `SIGID.Application.DTOs.Demand`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services` | Arma la serie diaria de ventas de un producto, usada por el análisis (HU-18), la predicción (HU-19) y la evaluación (HU-21). |
| `DemandProfileRules` **(Opt)** | `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand` | Clasifica el comportamiento de las ventas según la proporción de días con ventas. |
| `ForecastMetrics` **(S4)** | `SIGID.Application.DTOs.Demand` | Métricas de precisión (HU-21). Fórmulas, con e = real − estimado y n días evaluados: - MAE  = Σ|e| / n - RMSE = √(Σe² / n) - WAPE = Σ|e| / Σ|real| × 100 (no se puede calcular si en el período no hubo ventas). |
| `SalesAnalysisService` **(S4)** | `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services` | Análisis histórico de ventas por producto (HU-18). Solo consulta: no modifica nada. |
| `SyntheticSalesService` **(S4)** | `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Entities`, `SIGID.Domain.Enums` | Genera y consulta datos históricos de ventas de prueba (HU-17). Los datos se guardan en tablas propias: no crean ventas, no cambian existencias ni registran movimientos de inventario. |
| `TrainingPeriodRange` **(Opt)** | `SIGID.Application.Configuration`, `SIGID.Application.DTOs.Demand` | Convierte la opción «Historial de entrenamiento» en fechas concretas (predicción y evaluación). |

## Services/Inventory

| Clase | `using` | Uso |
|---|---|---|
| `InventoryService` **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Sales`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Entities`, `SIGID.Domain.Enums` | Lógica de negocio de entradas, salidas e historial de inventario (HU-08, HU-09 y HU-10). También procesa las salidas generadas por las ventas (HU-15) con las mismas reglas. |

## Services/Products

| Clase | `using` | Uso |
|---|---|---|
| `ProductService` | `SIGID.Application.DTOs`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Entities` | Lógica de negocio de productos (HU-05). |

## Services/Sales

| Clase | `using` | Uso |
|---|---|---|
| `SaleService` **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Sales`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Persistence`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Entities` | Registro de ventas (HU-14), actualización del inventario por ventas (HU-15) e historial de ventas (HU-16). |

## Services/Suppliers

| Clase | `using` | Uso |
|---|---|---|
| `SupplierService` | `System.Text.RegularExpressions`, `SIGID.Application.DTOs`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Repositories`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Entities` | Lógica de negocio de proveedores (HU-07). |
