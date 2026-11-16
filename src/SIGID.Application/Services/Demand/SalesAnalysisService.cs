using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Análisis histórico de ventas por producto (HU-18). Solo consulta: no modifica nada.
/// </summary>
public class SalesAnalysisService : ISalesAnalysisService
{
    /// <summary>
    /// Criterio de tendencia: si la recta de tendencia sube o baja menos del 10 % respecto
    /// al promedio a lo largo del período, se considera estable.
    /// </summary>
    public const double TrendThreshold = 0.10;

    /// <summary>
    /// Períodos mínimos para calcular una tendencia.
    /// </summary>
    public const int MinimumPeriodsForTrend = 3;

    private readonly IDemandHistoryService _historyService;
    private readonly IProductRepository _productRepository;
    private readonly DemandForecastOptions _options;
    private readonly TimeProvider _timeProvider;

    public SalesAnalysisService(
        IDemandHistoryService historyService,
        IProductRepository productRepository,
        DemandForecastOptions options,
        TimeProvider timeProvider)
    {
        _historyService = historyService;
        _productRepository = productRepository;
        _options = options;
        _timeProvider = timeProvider;
    }

    public async Task<SalesAnalysisResultDto> AnalyzeAsync(SalesAnalysisFilterDto filter, CancellationToken cancellationToken = default)
    {
        var result = new SalesAnalysisResultDto
        {
            GroupBy = filter.GroupBy,
            DataSource = filter.DataSource
        };

        var error = Validate(filter);
        if (error is not null)
            return Fail(result, error);

        var product = await _productRepository.GetByIdAsync(filter.ProductId!.Value, cancellationToken);
        if (product is null)
            return Fail(result, "El producto seleccionado no existe.");

        result.ProductId = product.Id;
        result.ProductName = product.Name;
        result.UnitOfMeasure = product.UnitOfMeasure;
        result.StartDate = filter.StartDate!.Value.Date;
        result.EndDate = filter.EndDate!.Value.Date;

        DemandSeriesDto series;
        try
        {
            series = await _historyService.GetDailySeriesAsync(product.Id, filter.DataSource, result.StartDate, result.EndDate, cancellationToken);
        }
        catch (BusinessException ex)
        {
            return Fail(result, ex.Message);
        }

        result.IsSuccessful = true;
        result.KnownStartDate = series.StartDate;
        result.KnownEndDate = series.EndDate;

        if (series.Points.Count == 0)
        {
            result.Notice = "No se encontraron datos de ventas para el período seleccionado.";
            return result;
        }

        result.Notice = BuildNotice(result, series);
        result.Periods = GroupPeriods(series.Points, filter.GroupBy);

        // Sin ventas no se calculan indicadores: un promedio o una tendencia en 0 serían engañosos.
        if (series.TotalQuantity <= 0)
        {
            result.Notice = "Este producto todavía no tiene ventas registradas para el período seleccionado.";
            return result;
        }

        result.HasSales = true;
        CalculateIndicators(result);
        return result;
    }

    private string? Validate(SalesAnalysisFilterDto filter)
    {
        var today = _timeProvider.GetLocalNow().Date;

        if (filter.ProductId is null or <= 0)
            return "Selecciona un producto para continuar.";

        if (filter.StartDate is null || filter.EndDate is null)
            return "Indica la fecha inicial y la fecha final.";

        if (filter.StartDate.Value.Date > filter.EndDate.Value.Date)
            return "La fecha inicial no puede ser posterior a la fecha final. Revisa el período seleccionado.";

        if (filter.EndDate.Value.Date > today)
            return "El período no puede incluir fechas futuras. La fecha final debe ser hoy o anterior.";

        if ((filter.EndDate.Value.Date - filter.StartDate.Value.Date).Days + 1 > _options.MaxAnalysisDays)
            return $"El período no puede superar los {_options.MaxAnalysisDays} días. Acorta el rango de fechas.";

        if (!Enum.IsDefined(filter.GroupBy))
            return "Selecciona una agrupación válida: por día, por semana o por mes.";

        if (!Enum.IsDefined(filter.DataSource))
            return "Selecciona un origen de datos válido.";

        return null;
    }

    private static string? BuildNotice(SalesAnalysisResultDto result, DemandSeriesDto series)
    {
        var notes = new List<string>();

        if (series.StartDate > result.StartDate || series.EndDate < result.EndDate)
            notes.Add($"Solo hay información del {series.StartDate:dd/MM/yyyy} al {series.EndDate:dd/MM/yyyy}; el resto del período no se tiene en cuenta.");

        if (series.UnknownDayCount > 0)
            notes.Add($"Hay {series.UnknownDayCount} días sin información dentro del período; no se cuentan como días sin ventas.");

        return notes.Count == 0 ? null : string.Join(" ", notes);
    }

    /// <summary>
    /// Suma las cantidades diarias por día, semana (de lunes a domingo) o mes.
    /// Los días conocidos sin ventas ya vienen en 0, así que los períodos sin ventas aparecen con 0.
    /// </summary>
    internal static List<SalesAnalysisPeriodDto> GroupPeriods(IEnumerable<DailyDemandPointDto> points, SalesGrouping groupBy)
    {
        var periods = points
            .GroupBy(p => GetPeriodStart(p.Date, groupBy))
            .OrderBy(g => g.Key)
            .Select(g => new SalesAnalysisPeriodDto
            {
                Period = g.Key,
                TotalQuantity = g.Sum(p => p.Quantity),
                DayCount = g.Count(),
                IsPartial = g.Count() < GetPeriodLength(g.Key, groupBy)
            })
            .ToList();

        for (var i = 1; i < periods.Count; i++)
            periods[i].ChangeFromPrevious = periods[i].TotalQuantity - periods[i - 1].TotalQuantity;

        return periods;
    }

    private static int GetPeriodLength(DateTime start, SalesGrouping groupBy) => groupBy switch
    {
        SalesGrouping.Week => 7,
        SalesGrouping.Month => DateTime.DaysInMonth(start.Year, start.Month),
        _ => 1
    };

    internal static DateTime GetPeriodStart(DateTime date, SalesGrouping groupBy) => groupBy switch
    {
        SalesGrouping.Week => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),   // lunes de esa semana
        SalesGrouping.Month => new DateTime(date.Year, date.Month, 1),
        _ => date.Date
    };

    internal static void CalculateIndicators(SalesAnalysisResultDto result)
    {
        result.TotalQuantity = result.Periods.Sum(p => p.TotalQuantity);

        // Una semana o un mes incompleto tiene menos días y bajaría el promedio, el mínimo y la
        // tendencia. Si hay períodos completos, los indicadores se calculan solo con ellos.
        var periods = result.Periods.Any(p => !p.IsPartial)
            ? result.Periods.Where(p => !p.IsPartial).ToList()
            : result.Periods;
        result.IndicatorPeriodCount = periods.Count;

        // El promedio incluye los períodos sin ventas.
        result.AverageQuantity = periods.Count == 0 ? 0 : periods.Sum(p => p.TotalQuantity) / periods.Count;

        var maximum = periods.OrderByDescending(p => p.TotalQuantity).ThenBy(p => p.Period).First();
        var minimum = periods.OrderBy(p => p.TotalQuantity).ThenBy(p => p.Period).First();
        result.MaximumQuantity = maximum.TotalQuantity;
        result.MaximumPeriod = maximum.Period;
        result.MinimumQuantity = minimum.TotalQuantity;
        result.MinimumPeriod = minimum.Period;

        (result.Trend, result.TrendChange) = CalculateTrend(periods.Select(p => p.TotalQuantity).ToList());
    }

    /// <summary>
    /// Tendencia por regresión lineal sobre las cantidades de cada período.
    /// Cambio relativo = pendiente × (períodos − 1) / promedio.
    /// Más de +10 %: creciente; menos de −10 %: decreciente; en otro caso, estable.
    /// Describe lo que ya pasó; no es una predicción.
    /// </summary>
    internal static (SalesTrend Trend, double? Change) CalculateTrend(IReadOnlyList<double> values)
    {
        if (values.Count < MinimumPeriodsForTrend)
            return (SalesTrend.NotEnoughData, null);

        var average = values.Average();
        if (average <= 0)
            return (SalesTrend.NotEnoughData, null);

        var n = values.Count;
        var meanX = (n - 1) / 2.0;
        double numerator = 0, denominator = 0;
        for (var i = 0; i < n; i++)
        {
            numerator += (i - meanX) * (values[i] - average);
            denominator += (i - meanX) * (i - meanX);
        }

        var slope = numerator / denominator;
        var change = slope * (n - 1) / average;

        var trend = change >= TrendThreshold ? SalesTrend.Increasing
            : change <= -TrendThreshold ? SalesTrend.Decreasing
            : SalesTrend.Stable;

        return (trend, Math.Round(change, 4));
    }

    private static SalesAnalysisResultDto Fail(SalesAnalysisResultDto result, string message)
    {
        result.IsSuccessful = false;
        result.ErrorMessage = message;
        return result;
    }
}
