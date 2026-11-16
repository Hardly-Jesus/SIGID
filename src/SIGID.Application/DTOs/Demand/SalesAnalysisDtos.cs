namespace SIGID.Application.DTOs.Demand;

public enum SalesGrouping
{
    Day = 1,
    Week = 2,
    Month = 3
}

public enum SalesTrend
{
    /// <summary>No hay suficientes períodos para calcular una tendencia.</summary>
    NotEnoughData = 0,
    Stable = 1,
    Increasing = 2,
    Decreasing = 3
}

/// <summary>
/// Filtros del análisis histórico de ventas (HU-18).
/// </summary>
public class SalesAnalysisFilterDto
{
    public int? ProductId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public SalesGrouping GroupBy { get; set; } = SalesGrouping.Day;

    public SalesDataSource DataSource { get; set; } = SalesDataSource.Real;
}

/// <summary>
/// Cantidad vendida en un período (día, semana o mes).
/// </summary>
public class SalesAnalysisPeriodDto
{
    /// <summary>
    /// Primer día del período.
    /// </summary>
    public DateTime Period { get; set; }

    public double TotalQuantity { get; set; }

    /// <summary>
    /// Diferencia con el período anterior (null en el primero).
    /// </summary>
    public double? ChangeFromPrevious { get; set; }

    /// <summary>
    /// Días con información dentro del período.
    /// </summary>
    public int DayCount { get; set; }

    /// <summary>
    /// Semana o mes incompleto (el rango o la información disponible no lo cubre entero).
    /// </summary>
    public bool IsPartial { get; set; }
}

/// <summary>
/// Resultado del análisis histórico de un producto.
/// </summary>
public class SalesAnalysisResultDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>
    /// Período que realmente tiene información (puede ser más corto que el pedido).
    /// </summary>
    public DateTime? KnownStartDate { get; set; }

    public DateTime? KnownEndDate { get; set; }

    public SalesGrouping GroupBy { get; set; }

    public SalesDataSource DataSource { get; set; }

    public double TotalQuantity { get; set; }

    public double AverageQuantity { get; set; }

    /// <summary>
    /// Períodos usados para el promedio, el máximo, el mínimo y la tendencia
    /// (los períodos completos, si los hay).
    /// </summary>
    public int IndicatorPeriodCount { get; set; }

    public double MaximumQuantity { get; set; }

    public double MinimumQuantity { get; set; }

    public DateTime? MaximumPeriod { get; set; }

    public DateTime? MinimumPeriod { get; set; }

    public SalesTrend Trend { get; set; }

    /// <summary>
    /// Cambio relativo estimado entre el inicio y el final del período (por ejemplo 0.25 = +25 %).
    /// </summary>
    public double? TrendChange { get; set; }

    public List<SalesAnalysisPeriodDto> Periods { get; set; } = new();

    /// <summary>
    /// true cuando hay al menos una venta en el período: solo entonces se muestran los indicadores.
    /// </summary>
    public bool HasSales { get; set; }

    public bool IsSuccessful { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Aviso informativo (por ejemplo, que el período se acortó al tramo con información).
    /// </summary>
    public string? Notice { get; set; }
}
