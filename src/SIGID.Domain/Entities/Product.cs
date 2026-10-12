using SIGID.Domain.Enums;

namespace SIGID.Domain.Entities;

/// <summary>
/// Producto gestionado por SIGID.
/// </summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>
    /// Código único del producto (Stock Keeping Unit).
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public int SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    /// <summary>
    /// Unidad de medida (por ejemplo: UND, KG, LT, CAJA).
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Umbral de referencia: por debajo o igual a este valor el stock se considera bajo.
    /// </summary>
    public int MinimumStock { get; set; }

    /// <summary>
    /// Umbral de referencia del stock máximo recomendado.
    /// </summary>
    public int MaximumStock { get; set; }

    /// <summary>
    /// Existencias actuales (unidades disponibles). Un producto nuevo empieza en 0
    /// y solo cambia mediante entradas y salidas de inventario.
    /// </summary>
    public int CurrentStock { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();

    /// <summary>
    /// Sin existencias: 0 unidades. Bajo: quedan unidades, pero en el mínimo o por debajo.
    /// </summary>
    public StockStatus GetStockStatus() => GetStockStatus(CurrentStock, MinimumStock);

    public static StockStatus GetStockStatus(int currentStock, int minimumStock)
    {
        if (currentStock <= 0)
            return StockStatus.OutOfStock;

        return currentStock <= minimumStock ? StockStatus.Low : StockStatus.Normal;
    }
}
