using SIGID.Domain.Enums;

namespace SIGID.Application.DTOs;

/// <summary>
/// Objeto de transferencia de datos de producto entre la capa Web y Application.
/// </summary>
public class ProductDto
{
    public int Id { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public int SupplierId { get; set; }

    public string? SupplierName { get; set; }

    public string UnitOfMeasure { get; set; } = string.Empty;

    public int MinimumStock { get; set; }

    public int MaximumStock { get; set; }

    /// <summary>
    /// Existencias actuales. Solo lectura desde el formulario de productos;
    /// se modifica con entradas y salidas de inventario.
    /// </summary>
    public int CurrentStock { get; set; }

    public StockStatus StockStatus { get; set; }

    public bool IsActive { get; set; } = true;
}
