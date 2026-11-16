namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Resultado de comparar SSA con el modelo de referencia, según el MAE del período evaluado.
/// </summary>
public enum ModelComparison
{
    SsaBetter = 1,
    Equal = 2,
    BaselineBetter = 3
}

/// <summary>
/// Solicitud de evaluación de precisión (HU-21).
/// </summary>
public class DemandForecastEvaluationRequestDto
{
    public int? ProductId { get; set; }

    public SalesDataSource DataSource { get; set; } = SalesDataSource.Real;

    /// <summary>
    /// Historial de entrenamiento elegido. Los períodos relativos se cuentan hacia atrás desde
    /// el día anterior al período evaluado.
    /// </summary>
    public TrainingPeriod TrainingPeriod { get; set; } = TrainingPeriod.Last12Months;

    /// <summary>
    /// Fechas del entrenamiento personalizado (solo con TrainingPeriod.Custom). La evaluación
    /// son los días que siguen a la fecha final.
    /// </summary>
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Días más recientes que se reservan para evaluar.
    /// </summary>
    public int? EvaluationHorizon { get; set; }
}

/// <summary>
/// Comparación de un día del período evaluado.
/// </summary>
public class DemandForecastEvaluationPeriodDto
{
    public DateTime Date { get; set; }

    public double ActualQuantity { get; set; }

    public double PredictedQuantity { get; set; }

    public double AbsoluteError { get; set; }

    /// <summary>
    /// Estimación del modelo de referencia (último valor observado).
    /// </summary>
    public double BaselineQuantity { get; set; }
}

/// <summary>
/// Métricas de un modelo. WAPE es null cuando no se puede calcular.
/// </summary>
public class ForecastMetricsDto
{
    public double MAE { get; set; }

    public double RMSE { get; set; }

    public double? WAPE { get; set; }
}

/// <summary>
/// Resultado de la evaluación de precisión de un producto.
/// </summary>
public class DemandForecastEvaluationResultDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public SalesDataSource DataSource { get; set; }

    public TrainingPeriod TrainingPeriod { get; set; }

    /// <summary>
    /// Período de entrenamiento pedido (el usado puede empezar después si no hay información antes).
    /// </summary>
    public DateTime? RequestedTrainingStartDate { get; set; }

    public DateTime? TrainingStartDate { get; set; }

    public DateTime? TrainingEndDate { get; set; }

    public DateTime? EvaluationStartDate { get; set; }

    public DateTime? EvaluationEndDate { get; set; }

    public int TrainingObservationCount { get; set; }

    public int EvaluationObservationCount { get; set; }

    public double? MAE { get; set; }

    public double? RMSE { get; set; }

    public double? WAPE { get; set; }

    public double? BaselineMAE { get; set; }

    public double? BaselineRMSE { get; set; }

    public double? BaselineWAPE { get; set; }

    public ModelComparison? Comparison { get; set; }

    /// <summary>
    /// Totales del período evaluado: lo vendido y lo estimado por cada modelo.
    /// </summary>
    public double TotalActualQuantity { get; set; }

    public double TotalPredictedQuantity { get; set; }

    public double TotalBaselineQuantity { get; set; }

    /// <summary>
    /// true si SSA no superó a la referencia, si su WAPE llegó al 100 % o si estimó 0
    /// en todos los días habiendo ventas.
    /// </summary>
    public bool IsUnfavorable { get; set; }

    /// <summary>
    /// true si SSA estimó 0 en todos los días evaluados.
    /// </summary>
    public bool IsZeroForecast { get; set; }

    /// <summary>
    /// Parámetros de SSA usados en el entrenamiento.
    /// </summary>
    public int WindowSize { get; set; }

    public int SeriesLength { get; set; }

    public DemandProfileDto? Profile { get; set; }

    public List<DemandForecastEvaluationPeriodDto> EvaluationPeriods { get; set; } = new();

    public DemandDataAvailabilityDto? Availability { get; set; }

    public bool IsSuccessful { get; set; }

    public bool IsTechnicalError { get; set; }

    public string? ErrorMessage { get; set; }
}
