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

    public int MinimumStock { get; set; }

    public int MaximumStock { get; set; }

    public bool IsActive { get; set; } = true;
}
