# SIGID.Web

Capa de presentación: ASP.NET Core MVC con vistas Razor, controladores, ViewModels, seguridad por cookies y estilos.

Inventario de dependencias: por carpeta y clase, los espacios de nombres que importa con `using`, los paquetes NuGet y su uso. Las marcadas con **(S4)** se agregaron o cambiaron en el Sprint 4 (EP-05) y las marcadas con **(Opt)**, en la optimización de la EP-05.

## Paquetes y referencias

SDK `Microsoft.NET.Sdk.Web` (incluye el framework `Microsoft.AspNetCore.App`). Referencias de proyecto: `SIGID.Application`, `SIGID.Infrastructure`.

| Paquete NuGet | Versión | Uso |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.0 | Comandos de migraciones (`Add-Migration`, `Update-Database`, `dotnet ef`). Solo en tiempo de desarrollo. |

## Controllers

| Clase | `using` | Uso |
|---|---|---|
| `AccountController` | `System.Security.Claims`, `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Authentication.Cookies`, `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Web.ViewModels`, `AdminAuthenticationService = SIGID.Application.Interfaces.Services.IAuthenticationService` | Inicio y cierre de sesión del administrador (HU-12). La verificación de credenciales la hace IAuthenticationService. |
| `CategoriesController` | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Gestión de categorías, HU-06 (crear, consultar, editar y eliminar). |
| `DashboardController` | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Interfaces.Services` | Dashboard con indicadores calculados a partir de los datos reales. |
| `DemandForecastController` **(S4)** **(Opt)** | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Configuration`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Consulta de predicciones de demanda (HU-20). El pronóstico lo calcula IDemandForecastService (modelo SSA de la HU-19); este controlador solo recibe los filtros y prepara la vista. |
| `ForecastEvaluationController` **(S4)** | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Configuration`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Evaluación de la precisión del modelo predictivo (HU-21). La evaluación la hace IDemandForecastEvaluationService; aquí solo se reciben los filtros y se muestra el resultado. |
| `HomeController` | `System.Diagnostics`, `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc` | Página de inicio y página de error. |
| `InventoryController` **(Opt)** | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Services`, `SIGID.Domain.Enums`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Entradas (HU-08), salidas (HU-09) e historial de movimientos (HU-10). Las reglas de existencias se validan en InventoryService. |
| `ProductsController` | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `Microsoft.AspNetCore.Mvc.Rendering`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Gestión de productos (HU-05). Flujo: View → Controller → Service (Application) → Repository (Infrastructure) → SQL Server. |
| `SalesAnalysisController` **(S4)** | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Análisis histórico de ventas por producto (HU-18). Solo consulta: no modifica ventas ni existencias. |
| `SalesController` **(Opt)** | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.DTOs.Sales`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Registro de ventas (HU-14 / HU-15) e historial de ventas (HU-16). Las reglas de la venta y la actualización de existencias están en SaleService. |
| `SuppliersController` | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.Extensions`, `SIGID.Web.ViewModels` | Gestión de proveedores, HU-07 (crear, consultar, editar y eliminar). |
| `SyntheticDataController` **(S4)** | `Microsoft.AspNetCore.Authorization`, `Microsoft.AspNetCore.Mvc`, `SIGID.Application.Configuration`, `SIGID.Application.Interfaces.Services`, `SIGID.Web.ViewModels` | Herramienta de desarrollo para generar datos históricos de prueba (HU-17). Solo existe cuando SyntheticData:Enabled es true y la aplicación corre en Development; en cualquier otro caso responde 404. Requiere sesión iniciada. |

## Extensions

| Clase | `using` | Uso |
|---|---|---|
| `DemandCharts` **(S4)** | `SIGID.Application.DTOs.Demand`, `SIGID.Web.ViewModels` | Prepara los datos de los gráficos a partir de los resultados de los servicios. Solo reordena valores para dibujarlos; no los modifica. |
| `DemandFormat` **(S4)** **(Opt)** | `System.Globalization`, `SIGID.Application.DTOs.Demand`, `SIGID.Domain.Enums` | Textos y formatos de las pantallas de demanda (EP-05). |
| `ModelStateExtensions` | `Microsoft.AspNetCore.Mvc.ModelBinding`, `SIGID.Application.Exceptions` | Muestra los errores de negocio en el formulario. |
| `QuantityFormat` | — | Formatea cantidades de varios productos sin sumar unidades de medida distintas. |

## (raíz)

| Clase | `using` | Uso |
|---|---|---|
| `Program` **(S4)** | `Microsoft.AspNetCore.Authentication.Cookies`, `Microsoft.AspNetCore.Authorization`, `Microsoft.Extensions.Options`, `SIGID.Application.Configuration`, `SIGID.Application.Interfaces.Services`, `SIGID.Application.Services.Authentication`, `SIGID.Application.Services.Categories`, `SIGID.Application.Services.Dashboard`, `SIGID.Application.Services.Demand`, `SIGID.Application.Services.Inventory`, `SIGID.Application.Services.Products`, `SIGID.Application.Services.Sales`, `SIGID.Application.Services.Suppliers`, `SIGID.Infrastructure`, `SIGID.Web.Security` | Arranque de la aplicación: MVC, servicios, autenticación, autorización y opciones de la EP-05. |

## Security

| Clase | `using` | Uso |
|---|---|---|
| `AdministratorCookieEvents` | `System.Security.Claims`, `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Authentication.Cookies`, `AdminAuthenticationService = SIGID.Application.Interfaces.Services.IAuthenticationService` | Eventos de la cookie de autenticación. |
| `InitialAdministratorSetup` | `SIGID.Application.DTOs.Authentication`, `SIGID.Application.Exceptions`, `SIGID.Application.Interfaces.Services` | Crea la cuenta del primer administrador al iniciar la aplicación, a partir de la sección "InitialAdministrator" de la configuración (User Secrets o variables de entorno; nunca appsettings.json). Solo actúa si todavía no existe ningún administrador. |
| `MemorySessionTicketStore` | `Microsoft.AspNetCore.Authentication`, `Microsoft.AspNetCore.Authentication.Cookies`, `Microsoft.Extensions.Caching.Memory` | Guarda la sesión del administrador en el servidor; la cookie solo lleva una clave aleatoria. Al cerrar sesión la entrada se elimina, por lo que una copia anterior de la cookie deja de servir. |

## ViewModels

| Clase | `using` | Uso |
|---|---|---|
| `CategoryViewModel` | `System.ComponentModel.DataAnnotations`, `SIGID.Application.DTOs` | Formulario de categorías. |
| `DemandChartViewModel` **(S4)** | — | Tipo de línea del gráfico. Cada tipo tiene su color y estilo en site.css. |
| `DemandForecastFilterViewModel` **(S4)** **(Opt)** | `SIGID.Application.DTOs.Demand` | Parámetros de la consulta de predicciones (HU-20). |
| `DemandForecastPeriodViewModel` **(S4)** | — | Un día pronosticado, tal como lo devuelve el servicio predictivo. |
| `DemandForecastViewModel` **(S4)** **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Demand` | Pantalla de consulta de predicciones de demanda (HU-20). Los valores se copian del resultado del servicio; aquí no se calcula ningún pronóstico. |
| `ForecastEvaluationViewModel` **(S4)** **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Demand` | Filtros y resultados de la evaluación de precisión del modelo (HU-21). |
| `LoginViewModel` | `System.ComponentModel.DataAnnotations`, `SIGID.Application.DTOs.Authentication` | Formulario de inicio de sesión (HU-12). |
| `MovementFormViewModel` | `System.ComponentModel.DataAnnotations`, `SIGID.Application.DTOs`, `SIGID.Domain.Enums` | Formulario de registro de una entrada o salida de inventario. |
| `MovementHistoryViewModel` **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Domain.Enums` | Filtros y resultados del historial de movimientos. |
| `MovementListViewModel` **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Domain.Enums` | Listado de entradas o de salidas de inventario. |
| `PagerViewModel` **(Opt)** | — | Datos de la paginación de un historial (se conserva el resto de los filtros en los enlaces). |
| `ProductViewModel` | `System.ComponentModel.DataAnnotations`, `Microsoft.AspNetCore.Mvc.Rendering`, `SIGID.Application.DTOs`, `SIGID.Domain.Enums` | Modelo de la vista de productos con las validaciones del formulario. Las mismas reglas se vuelven a validar en ProductService. |
| `RegisterSaleDetailViewModel` | `System.ComponentModel.DataAnnotations`, `SIGID.Application.DTOs` | Una línea de la venta: producto y cantidad. |
| `RegisterSaleViewModel` | `System.ComponentModel.DataAnnotations`, `Microsoft.AspNetCore.Mvc.ModelBinding`, `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Sales` | Formulario de registro de una venta (HU-14). |
| `SaleHistoryViewModel` **(Opt)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Sales` | Filtros y resultados del historial de ventas (HU-16). |
| `SalesAnalysisViewModel` **(S4)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Demand` | Filtros y resultados del análisis histórico de ventas (HU-18). |
| `SupplierViewModel` | `System.ComponentModel.DataAnnotations`, `SIGID.Application.DTOs` | Formulario de proveedores. |
| `SyntheticDataViewModel` **(S4)** | `SIGID.Application.DTOs`, `SIGID.Application.DTOs.Demand`, `SIGID.Domain.Enums` | Herramienta de desarrollo para generar datos históricos de prueba (HU-17). |
| `TrainingPeriodFieldsViewModel` **(Opt)** | `SIGID.Application.DTOs.Demand` | Datos del selector «Historial de entrenamiento» (compartido por Predicción y Precisión del modelo). |

## Views (Razor)

Las vistas importan por defecto lo indicado en `Views/_ViewImports.cshtml`. Se listan las directivas `@using` propias de cada vista.

| Vista | `@using` |
|---|---|
| `Views/Account/Login.cshtml` | — |
| `Views/Categories/Create.cshtml` | — |
| `Views/Categories/Delete.cshtml` | — |
| `Views/Categories/Edit.cshtml` | — |
| `Views/Categories/Index.cshtml` | — |
| `Views/Categories/_CategoryForm.cshtml` | — |
| `Views/Dashboard/Index.cshtml` | — |
| `Views/DemandForecast/Index.cshtml` **(S4)** **(Opt)** | `SIGID.Application.DTOs.Demand`, `SIGID.Web.Extensions` |
| `Views/ForecastEvaluation/Index.cshtml` **(S4)** **(Opt)** | `SIGID.Application.DTOs.Demand`, `SIGID.Web.Extensions` |
| `Views/Home/Index.cshtml` | — |
| `Views/Inventory/CreateEntry.cshtml` | — |
| `Views/Inventory/CreateExit.cshtml` | — |
| `Views/Inventory/Entries.cshtml` | — |
| `Views/Inventory/Exits.cshtml` | — |
| `Views/Inventory/History.cshtml` **(Opt)** | — |
| `Views/Inventory/_MovementForm.cshtml` | — |
| `Views/Inventory/_MovementList.cshtml` **(Opt)** | — |
| `Views/Products/Create.cshtml` | — |
| `Views/Products/Edit.cshtml` | — |
| `Views/Products/Index.cshtml` | — |
| `Views/Products/_ProductForm.cshtml` | — |
| `Views/Sales/Create.cshtml` | `SIGID.Web.Extensions` |
| `Views/Sales/Details.cshtml` | `SIGID.Web.Extensions` |
| `Views/Sales/Index.cshtml` **(Opt)** | `SIGID.Web.Extensions` |
| `Views/Sales/NotFound.cshtml` | — |
| `Views/SalesAnalysis/Index.cshtml` **(S4)** **(Opt)** | `SIGID.Application.DTOs.Demand`, `SIGID.Web.Extensions` |
| `Views/Shared/Error.cshtml` | — |
| `Views/Shared/_ActiveChip.cshtml` | — |
| `Views/Shared/_AuthLayout.cshtml` | — |
| `Views/Shared/_DataSourceNotice.cshtml` **(S4)** | `SIGID.Application.DTOs.Demand` |
| `Views/Shared/_DataSourceSelect.cshtml` **(S4)** | `SIGID.Application.DTOs.Demand` |
| `Views/Shared/_DemandChart.cshtml` **(S4)** | `System.Globalization`, `SIGID.Web.Extensions` |
| `Views/Shared/_DemandProductSelect.cshtml` **(S4)** | — |
| `Views/Shared/_Icons.cshtml` **(S4)** | — |
| `Views/Shared/_Layout.cshtml` **(S4)** | — |
| `Views/Shared/_MovementTable.cshtml` | — |
| `Views/Shared/_Pager.cshtml` **(Opt)** | `SIGID.Web.Extensions` |
| `Views/Shared/_StockChip.cshtml` | — |
| `Views/Shared/_StockMeter.cshtml` | — |
| `Views/Shared/_TrainingPeriodFields.cshtml` **(Opt)** | `SIGID.Application.DTOs.Demand`, `SIGID.Web.Extensions` |
| `Views/Suppliers/Create.cshtml` | — |
| `Views/Suppliers/Delete.cshtml` | — |
| `Views/Suppliers/Edit.cshtml` | — |
| `Views/Suppliers/Index.cshtml` | — |
| `Views/Suppliers/_SupplierForm.cshtml` | — |
| `Views/SyntheticData/Index.cshtml` **(S4)** | `SIGID.Domain.Enums`, `SIGID.Web.Extensions` |
| `Views/_ViewImports.cshtml` | `SIGID.Web`, `SIGID.Web.ViewModels`, `SIGID.Application.DTOs`, `SIGID.Domain.Enums` |
| `Views/_ViewStart.cshtml` | — |

## wwwroot

- `css/site.css`: estilos de SIGID. En el Sprint 4 y en la optimización solo se agregaron clases nuevas (gráfico, indicadores, evaluación, selector de historial, paginación); las existentes no cambiaron. **(S4)** **(Opt)**
- `js/site.js`: comportamientos simples de la interfaz. En la optimización se agregó mostrar u ocultar las fechas del historial personalizado; el cierre de avisos ya existía y se reutiliza. **(Opt)**
- No se usan bibliotecas JavaScript ni CSS externas: los gráficos se dibujan con SVG desde el servidor.
