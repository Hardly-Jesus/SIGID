namespace SIGID.Application.DTOs.Sales;

/// <summary>
/// Venta registrada con sus productos.
/// </summary>
public class SaleDto
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public List<SaleDetailDto> Details { get; set; } = new();
}

public class SaleDetailDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Existencia del producto después de descontar la venta.
    /// </summary>
    public int StockAfter { get; set; }
}
