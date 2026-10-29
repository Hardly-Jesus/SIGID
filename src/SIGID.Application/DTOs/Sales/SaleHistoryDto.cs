namespace SIGID.Application.DTOs.Sales;

/// <summary>
/// Fila del historial de ventas.
/// </summary>
public class SaleHistoryDto
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    /// <summary>
    /// Cantidad de productos diferentes incluidos en la venta.
    /// </summary>
    public int ProductCount { get; set; }

    /// <summary>
    /// Suma de las cantidades vendidas (solo tiene sentido si todos los productos usan la misma unidad).
    /// </summary>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Productos vendidos con su cantidad y unidad de medida, ordenados por nombre.
    /// </summary>
    public List<SaleHistoryItemDto> Items { get; set; } = new();
}

/// <summary>
/// Detalle completo de una venta para su consulta.
/// </summary>
public class SaleHistoryDetailDto
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public List<SaleHistoryItemDto> Details { get; set; } = new();

    public int TotalQuantity => Details.Sum(d => d.Quantity);
}

public class SaleHistoryItemDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

/// <summary>
/// Filtros del historial de ventas. Los valores nulos no filtran.
/// </summary>
public class SaleHistoryFilterDto
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? ProductId { get; set; }
}

/// <summary>
/// Cantidad vendida de un producto en un día. Información histórica para el análisis de demanda (EP-05).
/// </summary>
public class ProductSalesByDayDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public DateTime Day { get; set; }

    public int Quantity { get; set; }
}
