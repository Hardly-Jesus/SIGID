namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Cantidad vendida de un producto en un día.
/// </summary>
public class DailyDemandPointDto
{
    public DateTime Date { get; set; }

    public double Quantity { get; set; }
}

/// <summary>
/// Serie temporal diaria de un producto, lista para analizar o para entrenar el modelo.
/// Solo contiene días con información conocida; los días conocidos sin ventas valen 0.
/// </summary>
public class DemandSeriesDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public SalesDataSource DataSource { get; set; }

    /// <summary>
    /// Primer y último día con información dentro del período pedido (null si no hay ninguno).
    /// </summary>
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Días ordenados de forma cronológica, sin huecos entre StartDate y EndDate salvo los desconocidos.
    /// </summary>
    public List<DailyDemandPointDto> Points { get; set; } = new();

    /// <summary>
    /// Días dentro del período cuya información no se conoce (no se convierten en cero).
    /// </summary>
    public int UnknownDayCount { get; set; }

    /// <summary>
    /// Registros descartados por tener cantidades inválidas (negativas).
    /// </summary>
    public int InvalidRecordCount { get; set; }

    public int DaysWithSales => Points.Count(p => p.Quantity > 0);

    public double TotalQuantity => Points.Sum(p => p.Quantity);
}
