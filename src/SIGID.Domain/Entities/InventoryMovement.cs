using SIGID.Domain.Enums;

namespace SIGID.Domain.Entities;

/// <summary>
/// Movimiento de inventario (entrada o salida) de un producto.
/// Cada movimiento queda registrado para mantener la trazabilidad de las existencias.
/// </summary>
public class InventoryMovement
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public MovementType MovementType { get; set; }

    /// <summary>
    /// Cantidad de unidades del movimiento. Siempre es mayor que cero;
    /// el tipo indica si suma (Entrada) o resta (Salida).
    /// </summary>
    public int Quantity { get; set; }

    public DateTime Date { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Existencia del producto después de aplicar el movimiento.
    /// </summary>
    public int StockAfter { get; set; }

    /// <summary>
    /// Venta que generó el movimiento. Es nulo en las entradas y salidas manuales.
    /// </summary>
    public int? SaleId { get; set; }

    public Sale? Sale { get; set; }
}
