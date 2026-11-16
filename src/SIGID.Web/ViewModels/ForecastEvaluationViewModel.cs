using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Demand;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Filtros y resultados de la evaluación de precisión del modelo (HU-21).
/// </summary>
public class ForecastEvaluationViewModel
{
    public int? ProductId { get; set; }

    public SalesDataSource DataSource { get; set; } = SalesDataSource.Real;

    public int? EvaluationHorizon { get; set; }

    /// <summary>
    /// Historial de entrenamiento (por defecto, los 12 meses anteriores al período evaluado).
    /// </summary>
    public TrainingPeriod TrainingPeriod { get; set; } = TrainingPeriod.Last12Months;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int MinEvaluationDays { get; set; }

    public int MaxEvaluationDays { get; set; }

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    /// <summary>
    /// Resultado del servicio; null cuando todavía no se hizo ninguna consulta.
    /// </summary>
    public DemandForecastEvaluationResultDto? Result { get; set; }

    public DemandChartViewModel? Chart { get; set; }

    public string? ErrorMessage { get; set; }

    public DemandForecastEvaluationRequestDto ToRequest() => new()
    {
        ProductId = ProductId,
        DataSource = DataSource,
        TrainingPeriod = TrainingPeriod,
        StartDate = StartDate,
        EndDate = EndDate,
        EvaluationHorizon = EvaluationHorizon
    };
}
