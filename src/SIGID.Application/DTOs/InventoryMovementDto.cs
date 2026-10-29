using SIGID.Domain.Enums;

namespace SIGID.Application.DTOs;

/// <summary>
/// Movimiento de inventario listo para mostrarse en listados e historial.
/// </summary>
public class InventoryMovementDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductSku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public MovementType MovementType { get; set; }

    public int Quantity { get; set; }

    public DateTime Date { get; set; }

    public string? Description { get; set; }

    public int StockAfter { get; set; }

    /// <summary>
    /// Venta que originó el movimiento, si fue generado por una venta.
    /// </summary>
    public int? SaleId { get; set; }
}
