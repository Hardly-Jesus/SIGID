using SIGID.Application.DTOs.Demand;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Parámetros de la consulta de predicciones (HU-20).
/// </summary>
public class DemandForecastFilterViewModel
{
    public int? ProductId { get; set; }

    public SalesDataSource DataSource { get; set; } = SalesDataSource.Real;

    /// <summary>
    /// Historial de entrenamiento (por defecto, los últimos 12 meses).
    /// </summary>
    public TrainingPeriod TrainingPeriod { get; set; } = TrainingPeriod.Last12Months;

    /// <summary>
    /// Fechas del historial personalizado.
    /// </summary>
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? ForecastHorizon { get; set; }

    public DemandForecastRequestDto ToRequest() => new()
    {
        ProductId = ProductId,
        DataSource = DataSource,
        TrainingPeriod = TrainingPeriod,
        StartDate = StartDate,
        EndDate = EndDate,
        ForecastHorizon = ForecastHorizon
    };
}
