namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Estado del historial de un producto frente al modelo predictivo (HU-21).
/// </summary>
public enum DemandDataStatus
{
    /// <summary>No hay ventas en el período.</summary>
    NoHistory = 1,

    /// <summary>Hay ventas, pero no alcanzan para entrenar SSA.</summary>
    InsufficientHistory = 2,

    /// <summary>La serie tiene problemas: cantidades inválidas o días sin información.</summary>
    InvalidData = 3,

    /// <summary>Se puede pronosticar, pero no queda historial para reservar un período de evaluación.</summary>
    ForecastOnly = 4,

    /// <summary>Se puede pronosticar y también evaluar la precisión.</summary>
    Evaluable = 5
}

/// <summary>
/// Disponibilidad de datos de un producto para pronosticar y evaluar.
/// </summary>
public class DemandDataAvailabilityDto
{
    public int ProductId { get; set; }

    public SalesDataSource DataSource { get; set; }

    /// <summary>
    /// Días con información conocida (incluye días sin ventas).
    /// </summary>
    public int ObservationCount { get; set; }

    public int DaysWithSales { get; set; }

    /// <summary>
    /// Días necesarios para entrenar el modelo.
    /// </summary>
    public int RequiredObservationCount { get; set; }

    /// <summary>
    /// Días necesarios para entrenar y, además, reservar el período de evaluación.
    /// </summary>
    public int RequiredForEvaluationCount { get; set; }

    /// <summary>
    /// Días del período elegido por el usuario (con o sin información).
    /// </summary>
    public int RequestedDayCount { get; set; }

    /// <summary>
    /// Primer día con información dentro del período elegido.
    /// </summary>
    public DateTime? FirstKnownDate { get; set; }

    public bool CanForecast { get; set; }

    public bool CanEvaluate { get; set; }

    public DemandDataStatus Status { get; set; }

    public string Message { get; set; } = string.Empty;
}
