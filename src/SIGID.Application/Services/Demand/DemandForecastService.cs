using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Forecasting;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Coordina la predicción de demanda (HU-19): obtiene el historial, valida que alcance,
/// entrena el modelo SSA (a través de IDemandForecastEngine) y prepara los resultados.
/// No modifica ventas, existencias ni movimientos.
/// </summary>
public class DemandForecastService : IDemandForecastService
{
    public const string TechnicalErrorMessage = "No pudimos generar la predicción en este momento. Revisa los datos seleccionados e inténtalo nuevamente.";

    private readonly IDemandHistoryService _historyService;
    private readonly IProductRepository _productRepository;
    private readonly IDemandForecastEngine _engine;
    private readonly DemandForecastOptions _options;
    private readonly TimeProvider _timeProvider;

    public DemandForecastService(
        IDemandHistoryService historyService,
        IProductRepository productRepository,
        IDemandForecastEngine engine,
        DemandForecastOptions options,
        TimeProvider timeProvider)
    {
        _historyService = historyService;
        _productRepository = productRepository;
        _engine = engine;
        _options = options;
        _timeProvider = timeProvider;
    }

    public async Task<DemandForecastResultDto> ForecastAsync(DemandForecastRequestDto request, CancellationToken cancellationToken = default)
    {
        var today = _timeProvider.GetLocalNow().Date;
        var horizon = request.ForecastHorizon ?? _options.DefaultHorizonDays;

        var result = new DemandForecastResultDto
        {
            DataSource = request.DataSource,
            ReferenceDate = today,
            ForecastHorizon = horizon,
            TrainingPeriod = request.TrainingPeriod
        };

        if (request.ProductId is null or <= 0)
            return Fail(result, "Selecciona un producto para continuar.");

        if (!Enum.IsDefined(request.DataSource))
            return Fail(result, "Selecciona un origen de datos válido.");

        if (horizon < _options.MinHorizonDays || horizon > _options.MaxHorizonDays)
            return Fail(result, $"El período de predicción seleccionado no es válido. Elige una cantidad de días entre {_options.MinHorizonDays} y {_options.MaxHorizonDays}.");

        // Hoy no se usa: el día todavía no termina y sus ventas están incompletas.
        var range = TrainingPeriodRange.Resolve(request.TrainingPeriod, request.StartDate, request.EndDate, today.AddDays(-1), today, _options);
        if (range.Error is not null)
            return Fail(result, range.Error);

        result.RequestedStartDate = range.Start;
        result.RequestedEndDate = range.End;

        var product = await _productRepository.GetByIdAsync(request.ProductId.Value, cancellationToken);
        if (product is null)
            return Fail(result, "El producto seleccionado no existe.");

        result.ProductId = product.Id;
        result.ProductName = product.Name;
        result.UnitOfMeasure = product.UnitOfMeasure;

        // Solo se consulta el período elegido: el modelo nunca ve días fuera de él.
        DemandSeriesDto series;
        try
        {
            series = await _historyService.GetDailySeriesAsync(product.Id, request.DataSource, range.Start, range.End, cancellationToken);
        }
        catch (BusinessException ex)
        {
            return Fail(result, ex.Message);
        }

        result.HistoricalStartDate = series.StartDate;
        result.HistoricalEndDate = series.EndDate;
        result.TrainingObservationCount = series.Points.Count;
        result.HistoricalPeriods = series.Points.TakeLast(_options.RecentHistoryDays).ToList();

        var requestedDays = (range.End - range.Start).Days + 1;
        var availability = DemandDataRules.ForTraining(series, _options, horizon, requestedDays, _options.DefaultEvaluationDays);
        result.Availability = availability;
        result.Profile = DemandProfileRules.Classify(series.Points, _options, availability.CanForecast);

        // Evitamos mostrar resultados cuando el historial es insuficiente.
        if (!availability.CanForecast)
            return Fail(result, availability.Message);

        // El pronóstico empieza el día siguiente al último dato. Los días que ya pasaron
        // (incluido hoy, que todavía no termina) se calculan pero no se muestran como futuros.
        var lastHistoryDate = series.EndDate!.Value;
        var skippedDays = (today - lastHistoryDate).Days;
        if (skippedDays > _options.MaxHorizonDays)
            return Fail(result, $"La información histórica termina el {lastHistoryDate:dd/MM/yyyy}, demasiado lejos de hoy para pronosticar. Usa datos más recientes.");

        var steps = skippedDays + horizon;
        var training = series.Points.Select(p => (float)p.Quantity).ToList();

        ForecastEngineResult output;
        try
        {
            output = _engine.Forecast(training, steps);
        }
        catch (Exception)
        {
            return Fail(result, TechnicalErrorMessage, technical: true);
        }

        var periods = BuildPeriods(output, lastHistoryDate, skippedDays, steps);
        if (periods is null)
            return Fail(result, TechnicalErrorMessage, technical: true);

        result.ForecastPeriods = periods;
        result.ForecastStartDate = periods.First().Date;
        result.ForecastEndDate = periods.Last().Date;
        result.TotalPredictedQuantity = Math.Round(periods.Sum(p => p.PredictedQuantity), 2);
        result.AveragePredictedQuantity = Math.Round(result.TotalPredictedQuantity / periods.Count, 2);
        result.HasPredictionBounds = periods.All(p => p.LowerBound.HasValue && p.UpperBound.HasValue);
        result.IsZeroForecast = periods.All(p => p.PredictedQuantity == 0);
        result.WindowSize = output.WindowSize;
        result.SeriesLength = output.SeriesLength;
        result.IsSuccessful = true;
        return result;
    }

    /// <summary>
    /// Convierte la salida del modelo en días con fecha. Devuelve null si el resultado no es válido
    /// (cantidad incorrecta de valores, NaN o infinitos).
    /// </summary>
    internal static List<DemandForecastPeriodDto>? BuildPeriods(ForecastEngineResult output, DateTime lastHistoryDate, int skippedDays, int steps)
    {
        if (output.Forecast.Length != steps)
            return null;

        var periods = new List<DemandForecastPeriodDto>();
        for (var i = skippedDays; i < steps; i++)
        {
            var value = output.Forecast[i];
            if (!float.IsFinite(value))
                return null;

            // Regla: una demanda negativa no tiene sentido, se ajusta a 0.
            var predicted = Math.Round(Math.Max(0, (double)value), 2);

            double? lower = null, upper = null;
            if (i < output.LowerBound.Length && i < output.UpperBound.Length
                && float.IsFinite(output.LowerBound[i]) && float.IsFinite(output.UpperBound[i]))
            {
                lower = Math.Round(Math.Max(0, (double)output.LowerBound[i]), 2);
                upper = Math.Round(Math.Max(predicted, (double)output.UpperBound[i]), 2);
            }

            periods.Add(new DemandForecastPeriodDto
            {
                Date = lastHistoryDate.AddDays(i + 1),
                PredictedQuantity = predicted,
                LowerBound = lower,
                UpperBound = upper
            });
        }

        return periods;
    }

    public async Task<DemandDataAvailabilityDto> GetAvailabilityAsync(int productId, SalesDataSource dataSource, CancellationToken cancellationToken = default)
    {
        // Estado con las opciones por defecto: últimos 12 meses y horizonte por defecto.
        var today = _timeProvider.GetLocalNow().Date;
        var range = TrainingPeriodRange.Resolve(TrainingPeriod.Last12Months, null, null, today.AddDays(-1), today, _options);
        var series = await _historyService.GetDailySeriesAsync(productId, dataSource, range.Start, range.End, cancellationToken);
        return DemandDataRules.ForTraining(series, _options, _options.DefaultHorizonDays, (range.End - range.Start).Days + 1, _options.DefaultEvaluationDays);
    }

    private static DemandForecastResultDto Fail(DemandForecastResultDto result, string message, bool technical = false)
    {
        result.IsSuccessful = false;
        result.IsTechnicalError = technical;
        result.ErrorMessage = message;
        result.ForecastPeriods = new List<DemandForecastPeriodDto>();
        return result;
    }
}
