using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Demand;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Filtros y resultados del análisis histórico de ventas (HU-18).
/// </summary>
public class SalesAnalysisViewModel
{
    public int? ProductId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public SalesGrouping GroupBy { get; set; } = SalesGrouping.Day;

    public SalesDataSource DataSource { get; set; } = SalesDataSource.Real;

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    /// <summary>
    /// Resultado del servicio; null cuando todavía no se hizo ninguna consulta.
    /// </summary>
    public SalesAnalysisResultDto? Result { get; set; }

    public DemandChartViewModel? Chart { get; set; }

    public string? ErrorMessage { get; set; }

    public bool HasResults => Result is { IsSuccessful: true, Periods.Count: > 0 };

    public SalesAnalysisFilterDto ToFilter() => new()
    {
        ProductId = ProductId,
        StartDate = StartDate,
        EndDate = EndDate,
        GroupBy = GroupBy,
        DataSource = DataSource
    };
}
