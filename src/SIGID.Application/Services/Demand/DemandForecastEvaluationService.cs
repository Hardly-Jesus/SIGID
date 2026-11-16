using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Forecasting;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Application.Interfaces.Services;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Evalúa la precisión del modelo SSA con datos conocidos (HU-21).
/// Reserva los últimos días del historial, entrena solo con los anteriores, pronostica
/// los días reservados y compara con lo que realmente pasó y con un modelo de referencia.
/// </summary>
public class DemandForecastEvaluationService : IDemandForecastEvaluationService
{
    private readonly IDemandHistoryService _historyService;
    private readonly IProductRepository _productRepository;
    private readonly IDemandForecastEngine _engine;
    private readonly DemandForecastOptions _options;
    private readonly TimeProvider _timeProvider;

    public DemandForecastEvaluationService(
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

    public async Task<DemandForecastEvaluationResultDto> EvaluateAsync(DemandForecastEvaluationRequestDto request, CancellationToken cancellationToken = default)
    {
        var today = _timeProvider.GetLocalNow().Date;
        var yesterday = today.AddDays(-1);
        var evaluationDays = request.EvaluationHorizon ?? _options.DefaultEvaluationDays;
        var result = new DemandForecastEvaluationResultDto { DataSource = request.DataSource, TrainingPeriod = request.TrainingPeriod };

        if (request.ProductId is null or <= 0)
            return Fail(result, "Selecciona un producto para continuar.");

        if (!Enum.IsDefined(request.DataSource))
            return Fail(result, "Selecciona un origen de datos válido.");

        if (evaluationDays < _options.MinEvaluationDays || evaluationDays > _options.MaxEvaluationDays)
            return Fail(result, $"El período de evaluación no es válido. Elige entre {_options.MinEvaluationDays} y {_options.MaxEvaluationDays} días.");

        // El entrenamiento termina antes del período evaluado: los días evaluados nunca se usan para entrenar.
        // Con períodos relativos, la evaluación son los últimos días completos (hasta ayer).
        // Con un período personalizado, la evaluación son los días que siguen a la fecha final elegida.
        var lastTrainingDay = yesterday.AddDays(-evaluationDays);
        if (request.TrainingPeriod == TrainingPeriod.Custom && request.EndDate?.Date > lastTrainingDay && request.EndDate?.Date < today)
            return Fail(result, $"Para evaluar {evaluationDays} días después del entrenamiento, la fecha final del historial debe ser como máximo el {lastTrainingDay:dd/MM/yyyy}.");

        var range = TrainingPeriodRange.Resolve(request.TrainingPeriod, request.StartDate, request.EndDate, lastTrainingDay, today, _options);
        if (range.Error is not null)
            return Fail(result, range.Error);

        var evaluationStart = range.End.AddDays(1);
        var evaluationEnd = range.End.AddDays(evaluationDays);
        result.RequestedTrainingStartDate = range.Start;

        var product = await _productRepository.GetByIdAsync(request.ProductId.Value, cancellationToken);
        if (product is null)
            return Fail(result, "El producto seleccionado no existe.");

        result.ProductId = product.Id;
        result.ProductName = product.Name;
        result.UnitOfMeasure = product.UnitOfMeasure;

        DemandSeriesDto trainingSeries, evaluationSeries;
        try
        {
            trainingSeries = await _historyService.GetDailySeriesAsync(product.Id, request.DataSource, range.Start, range.End, cancellationToken);
            evaluationSeries = await _historyService.GetDailySeriesAsync(product.Id, request.DataSource, evaluationStart, evaluationEnd, cancellationToken);
        }
        catch (BusinessException ex)
        {
            return Fail(result, ex.Message);
        }

        // Antes de evaluar se revisa que el historial de entrenamiento alcance y sea válido.
        var requestedDays = (range.End - range.Start).Days + 1;
        var availability = DemandDataRules.ForTraining(trainingSeries, _options, evaluationDays, requestedDays, evaluationDays);
        result.Availability = availability;
        result.Profile = DemandProfileRules.Classify(trainingSeries.Points, _options, availability.CanForecast);

        if (!availability.CanForecast)
            return Fail(result, await NotEvaluableMessageAsync(availability, product.Id, request.DataSource, today, evaluationDays, cancellationToken));

        // El período evaluado debe tener información completa y seguir al entrenamiento sin huecos.
        if (evaluationSeries.Points.Count != evaluationDays || evaluationSeries.UnknownDayCount > 0 || evaluationSeries.InvalidRecordCount > 0
            || trainingSeries.EndDate != range.End)
        {
            availability.CanEvaluate = false;
            availability.Status = DemandDataStatus.InvalidData;
            return Fail(result, $"No hay información de ventas completa del {evaluationStart:dd/MM/yyyy} al {evaluationEnd:dd/MM/yyyy} para comparar con la estimación. Elige otro período.");
        }

        availability.CanEvaluate = true;
        availability.Status = DemandDataStatus.Evaluable;

        // Separamos los últimos días para evaluar la predicción (la división también revisa el orden).
        var (training, evaluation) = ChronologicalSplit.Split(
            trainingSeries.Points.Concat(evaluationSeries.Points).ToList(), evaluationDays, availability.RequiredObservationCount);

        result.TrainingStartDate = training.First().Date;
        result.TrainingEndDate = training.Last().Date;
        result.EvaluationStartDate = evaluation.First().Date;
        result.EvaluationEndDate = evaluation.Last().Date;
        result.TrainingObservationCount = training.Count;
        result.EvaluationObservationCount = evaluation.Count;

        // El modelo solo ve los días de entrenamiento: nunca los días que va a pronosticar.
        ForecastEngineResult output;
        try
        {
            output = _engine.Forecast(training.Select(p => (float)p.Quantity).ToList(), evaluationDays);
        }
        catch (Exception)
        {
            return Fail(result, "No pudimos evaluar el modelo en este momento. Revisa los datos seleccionados e inténtalo nuevamente.", technical: true);
        }

        if (output.Forecast.Length != evaluationDays || output.Forecast.Any(v => !float.IsFinite(v)))
            return Fail(result, "No pudimos evaluar el modelo porque el resultado del pronóstico no es válido. Inténtalo con otro período.", technical: true);

        var actual = evaluation.Select(p => p.Quantity).ToList();
        var predicted = output.Forecast.Select(v => Math.Max(0, (double)v)).ToList();   // negativos → 0, igual que en la predicción
        var baseline = ForecastMetrics.NaiveForecast(training.Select(p => p.Quantity).ToList(), evaluationDays);

        var ssaMetrics = ForecastMetrics.Calculate(actual, predicted);
        var baselineMetrics = ForecastMetrics.Calculate(actual, baseline);

        result.MAE = Math.Round(ssaMetrics.MAE, 4);
        result.RMSE = Math.Round(ssaMetrics.RMSE, 4);
        result.WAPE = ssaMetrics.WAPE is null ? null : Math.Round(ssaMetrics.WAPE.Value, 2);
        result.BaselineMAE = Math.Round(baselineMetrics.MAE, 4);
        result.BaselineRMSE = Math.Round(baselineMetrics.RMSE, 4);
        result.BaselineWAPE = baselineMetrics.WAPE is null ? null : Math.Round(baselineMetrics.WAPE.Value, 2);
        result.Comparison = ForecastMetrics.Compare(ssaMetrics.MAE, baselineMetrics.MAE);

        result.TotalActualQuantity = actual.Sum();
        result.TotalPredictedQuantity = Math.Round(predicted.Sum(), 2);
        result.TotalBaselineQuantity = baseline.Sum();
        result.IsZeroForecast = predicted.All(v => Math.Round(v, 2) == 0);
        result.IsUnfavorable = result.Comparison != ModelComparison.SsaBetter
            || ssaMetrics.WAPE >= 100
            || (result.IsZeroForecast && result.TotalActualQuantity > 0);
        result.WindowSize = output.WindowSize;
        result.SeriesLength = output.SeriesLength;

        for (var i = 0; i < evaluation.Count; i++)
        {
            result.EvaluationPeriods.Add(new DemandForecastEvaluationPeriodDto
            {
                Date = evaluation[i].Date,
                ActualQuantity = actual[i],
                PredictedQuantity = Math.Round(predicted[i], 2),
                AbsoluteError = Math.Round(Math.Abs(actual[i] - predicted[i]), 2),
                BaselineQuantity = baseline[i]
            });
        }

        result.IsSuccessful = true;
        return result;
    }

    /// <summary>
    /// Mensaje cuando el historial de entrenamiento no alcanza. Si el historial completo hasta ayer
    /// sí permite pronosticar, se informa que la predicción está disponible aunque no se pueda evaluar.
    /// </summary>
    private async Task<string> NotEvaluableMessageAsync(DemandDataAvailabilityDto availability, int productId, SalesDataSource source, DateTime today, int evaluationDays, CancellationToken cancellationToken)
    {
        if (availability.Status != DemandDataStatus.InsufficientHistory)
            return availability.Message;

        var range = TrainingPeriodRange.Resolve(TrainingPeriod.AllHistory, null, null, today.AddDays(-1), today, _options);
        var fullSeries = await _historyService.GetDailySeriesAsync(productId, source, range.Start, range.End, cancellationToken);
        var full = DemandDataRules.ForTraining(fullSeries, _options, _options.DefaultHorizonDays, (range.End - range.Start).Days + 1, evaluationDays);
        if (!full.CanForecast)
            return availability.Message;

        availability.Status = DemandDataStatus.ForecastOnly;
        return $"Todavía no hay suficiente información para evaluar la precisión de este producto con el historial elegido. " +
               $"Hay datos para pronosticar, pero para evaluar {evaluationDays} días se necesitan al menos {availability.RequiredObservationCount} días de entrenamiento " +
               $"antes del período evaluado y {_options.MinimumDaysWithSales} días con ventas. Prueba con un historial de entrenamiento más amplio.";
    }

    private static DemandForecastEvaluationResultDto Fail(DemandForecastEvaluationResultDto result, string message, bool technical = false)
    {
        result.IsSuccessful = false;
        result.IsTechnicalError = technical;
        result.ErrorMessage = message;
        return result;
    }
}
