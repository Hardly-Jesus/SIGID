using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Demand;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Pantalla de consulta de predicciones de demanda (HU-20).
/// Los valores se copian del resultado del servicio; aquí no se calcula ningún pronóstico.
/// </summary>
public class DemandForecastViewModel
{
    public DemandForecastFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    public int MinHorizon { get; set; }

    public int MaxHorizon { get; set; }

    /// <summary>
    /// Indica si ya se realizó una consulta (en la primera visita solo se muestran los filtros).
    /// </summary>
    public bool HasQuery { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string UnitOfMeasure { get; set; } = string.Empty;

    public SalesDataSource DataSource { get; set; }

    public DateTime ReferenceDate { get; set; }

    public TrainingPeriod TrainingPeriod { get; set; }

    public DateTime? RequestedStartDate { get; set; }

    public DateTime? RequestedEndDate { get; set; }

    public DateTime? HistoricalStartDate { get; set; }

    public DateTime? HistoricalEndDate { get; set; }

    public int TrainingObservationCount { get; set; }

    public DateTime? ForecastStartDate { get; set; }

    public DateTime? ForecastEndDate { get; set; }

    public int ForecastHorizon { get; set; }

    public int WindowSize { get; set; }

    public DemandProfileDto? Profile { get; set; }

    public bool IsZeroForecast { get; set; }

    public double TotalPredictedQuantity { get; set; }

    public double AveragePredictedQuantity { get; set; }

    public List<DailyDemandPointDto> HistoricalPeriods { get; set; } = new();

    public List<DemandForecastPeriodViewModel> ForecastPeriods { get; set; } = new();

    public bool HasPredictionBounds { get; set; }

    public DemandDataAvailabilityDto? Availability { get; set; }

    public DemandChartViewModel? Chart { get; set; }

    public bool IsSuccessful { get; set; }

    public bool IsTechnicalError { get; set; }

    public string? ErrorMessage { get; set; }
}
