namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Solicitud de predicción (HU-19 / HU-20).
/// </summary>
public class DemandForecastRequestDto
{
    public int? ProductId { get; set; }

    public SalesDataSource DataSource { get; set; } = SalesDataSource.Real;

    /// <summary>
    /// Historial de entrenamiento elegido (por defecto, los últimos 12 meses).
    /// </summary>
    public TrainingPeriod TrainingPeriod { get; set; } = TrainingPeriod.Last12Months;

    /// <summary>
    /// Fechas del historial personalizado (solo con TrainingPeriod.Custom).
    /// </summary>
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Días a pronosticar.
    /// </summary>
    public int? ForecastHorizon { get; set; }
}

/// <summary>
/// Estimación de un día.
/// </summary>
public class DemandForecastPeriodDto
{
    public DateTime Date { get; set; }

    public double PredictedQuantity { get; set; }

    /// <summary>
    /// Límites de predicción de SSA (null si no están disponibles o no son válidos).
    /// </summary>
    public double? LowerBound { get; set; }

    public double? UpperBound { get; set; }
}

/// <summary>
/// Resultado de una predicción. Las cantidades son estimaciones, no ventas confirmadas.
/// </summary>
public class DemandForecastResultDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public SalesDataSource DataSource { get; set; }

    /// <summary>
    /// Día desde el que se considera "futuro" (fecha de la consulta).
    /// </summary>
    public DateTime ReferenceDate { get; set; }

    public TrainingPeriod TrainingPeriod { get; set; }

    /// <summary>
    /// Período pedido por el usuario. El historial realmente usado puede empezar después
    /// si no hay información desde el comienzo del período.
    /// </summary>
    public DateTime? RequestedStartDate { get; set; }

    public DateTime? RequestedEndDate { get; set; }

    /// <summary>
    /// Historial realmente usado para entrenar.
    /// </summary>
    public DateTime? HistoricalStartDate { get; set; }

    public DateTime? HistoricalEndDate { get; set; }

    public int TrainingObservationCount { get; set; }

    /// <summary>
    /// Parámetros de SSA usados en el entrenamiento.
    /// </summary>
    public int WindowSize { get; set; }

    public int SeriesLength { get; set; }

    /// <summary>
    /// Comportamiento de las ventas en el historial de entrenamiento.
    /// </summary>
    public DemandProfileDto? Profile { get; set; }

    /// <summary>
    /// true si todos los días pronosticados quedaron en 0.
    /// </summary>
    public bool IsZeroForecast { get; set; }

    public DateTime? ForecastStartDate { get; set; }

    public DateTime? ForecastEndDate { get; set; }

    public int ForecastHorizon { get; set; }

    public double TotalPredictedQuantity { get; set; }

    public double AveragePredictedQuantity { get; set; }

    public List<DemandForecastPeriodDto> ForecastPeriods { get; set; } = new();

    /// <summary>
    /// Ventas recientes del mismo producto y origen, como referencia.
    /// </summary>
    public List<DailyDemandPointDto> HistoricalPeriods { get; set; } = new();

    public bool HasPredictionBounds { get; set; }

    /// <summary>
    /// Estado del historial (permite saber si además se puede evaluar la precisión).
    /// </summary>
    public DemandDataAvailabilityDto? Availability { get; set; }

    public bool IsSuccessful { get; set; }

    /// <summary>
    /// true cuando el fallo se debe a un error técnico del modelo y no a los datos.
    /// </summary>
    public bool IsTechnicalError { get; set; }

    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Valores que devuelve el motor de predicción (ML.NET/SSA).
/// </summary>
public class ForecastEngineResult
{
    public float[] Forecast { get; set; } = Array.Empty<float>();

    public float[] LowerBound { get; set; } = Array.Empty<float>();

    public float[] UpperBound { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Ventana de SSA usada en este entrenamiento (días).
    /// </summary>
    public int WindowSize { get; set; }

    /// <summary>
    /// Memoria de SSA usada en este entrenamiento (días).
    /// </summary>
    public int SeriesLength { get; set; }

    /// <summary>
    /// Promedio diario del entrenamiento: SSA trabaja con las diferencias respecto de este nivel.
    /// </summary>
    public float TrainingMean { get; set; }
}
