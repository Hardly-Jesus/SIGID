namespace SIGID.Domain.Entities;

/// <summary>
/// Cantidad vendida de un producto en un día, dentro de un lote de datos de prueba.
/// Los días sin registro dentro del período del lote significan demanda cero.
/// </summary>
public class SyntheticSalesRecord
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public SyntheticSalesBatch? Batch { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    /// <summary>
    /// Día de la venta (sin hora).
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Cantidad en la unidad de medida del producto. Siempre mayor que cero.
    /// </summary>
    public int Quantity { get; set; }
}
