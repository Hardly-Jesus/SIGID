# SIGID.Domain

Capa de dominio: entidades y enumeraciones del negocio. No depende de ninguna otra capa.

Inventario de dependencias: por carpeta y clase, los espacios de nombres que importa con `using`, los paquetes NuGet y su uso. Las marcadas con **(S4)** se agregaron o cambiaron en el Sprint 4 (EP-05) y las marcadas con **(Opt)**, en la optimización de la EP-05.

## Paquetes y referencias

No usa paquetes NuGet. Solo el SDK `Microsoft.NET.Sdk` (net9.0).

## Entities

| Clase | `using` | Uso |
|---|---|---|
| `Administrator` | — | Administrador con acceso a SIGID. Es el único tipo de usuario del sistema. |
| `Category` | — | Categoría a la que pertenecen los productos. Relación: Category 1 ──── N Product |
| `InventoryMovement` | `SIGID.Domain.Enums` | Movimiento de inventario (entrada o salida) de un producto. Cada movimiento queda registrado para mantener la trazabilidad de las existencias. |
| `Product` | `SIGID.Domain.Enums` | Producto gestionado por SIGID. |
| `Sale` | — | Venta registrada en el sistema. Cada venta tiene uno o varios productos (detalles). |
| `SaleDetail` | — | Producto incluido en una venta y la cantidad vendida. |
| `Supplier` | — | Proveedor de productos. Relación: Supplier 1 ──── N Product |
| `SyntheticSalesBatch` **(S4)** | `SIGID.Domain.Enums` | Lote de ventas históricas de prueba (datos sintéticos, HU-17). Se guarda aparte de las ventas reales: no es una venta ni mueve el inventario. |
| `SyntheticSalesRecord` **(S4)** | — | Cantidad vendida de un producto en un día, dentro de un lote de datos de prueba. Los días sin registro dentro del período del lote significan demanda cero. |

## Enums

| Clase | `using` | Uso |
|---|---|---|
| `DemandPattern` **(S4)** | — | Comportamiento de demanda usado para generar datos históricos de prueba (HU-17). |
| `MovementType` | — | Tipo de movimiento de inventario. Se guarda en la base de datos como texto ("Entry" / "Exit"). |
| `StockStatus` | — | Situación de las existencias de un producto respecto a su stock mínimo. |
