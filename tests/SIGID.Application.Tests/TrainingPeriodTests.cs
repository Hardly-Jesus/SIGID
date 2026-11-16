using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Services.Demand;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;

namespace SIGID.Application.Tests;

public class TrainingPeriodRangeTests
{
    private static readonly DateTime Today = new(2026, 11, 6);
    private static readonly DateTime Yesterday = Today.AddDays(-1);
    private readonly DemandForecastOptions _options = new();

    [Theory]
    [InlineData(TrainingPeriod.LastMonth, "2026-10-06")]
    [InlineData(TrainingPeriod.Last3Months, "2026-08-06")]
    [InlineData(TrainingPeriod.Last6Months, "2026-05-06")]
    [InlineData(TrainingPeriod.Last12Months, "2025-11-06")]
    [InlineData(TrainingPeriod.AllHistory, "2023-11-06")]
    public void RelativePeriods_EndOnLastAllowedDay(TrainingPeriod period, string expectedStart)
    {
        var (start, end, error) = TrainingPeriodRange.Resolve(period, null, null, Yesterday, Today, _options);

        Assert.Null(error);
        Assert.Equal(Yesterday, end);
        Assert.Equal(DateTime.Parse(expectedStart), start);
    }

    [Fact]
    public void RelativePeriods_CountBackFromTheGivenEnd()
    {
        // En la evaluación, el entrenamiento termina antes del período evaluado.
        var lastTrainingDay = Yesterday.AddDays(-14);
        var (start, end, _) = TrainingPeriodRange.Resolve(TrainingPeriod.Last3Months, null, null, lastTrainingDay, Today, _options);

        Assert.Equal(new DateTime(2026, 10, 22), end);
        Assert.Equal(new DateTime(2026, 7, 23), start);
    }

    [Fact]
    public void Custom_ValidRange_IsUsedAsIs()
    {
        var (start, end, error) = TrainingPeriodRange.Resolve(TrainingPeriod.Custom, new DateTime(2026, 3, 1), new DateTime(2026, 6, 30), Yesterday, Today, _options);

        Assert.Null(error);
        Assert.Equal(new DateTime(2026, 3, 1), start);
        Assert.Equal(new DateTime(2026, 6, 30), end);
    }

    [Theory]
    [InlineData(null, "2026-06-30", "Indica las fechas")]
    [InlineData("2026-07-01", "2026-06-30", "no puede ser posterior")]
    [InlineData("2026-10-01", "2026-11-06", "no puede incluir el día de hoy")]
    [InlineData("2026-10-01", "2026-12-01", "no puede incluir el día de hoy")]
    [InlineData("2022-01-01", "2026-10-01", "no puede superar")]
    public void Custom_InvalidRange_IsRejected(string? from, string? to, string expected)
    {
        var (_, _, error) = TrainingPeriodRange.Resolve(TrainingPeriod.Custom,
            from is null ? null : DateTime.Parse(from), to is null ? null : DateTime.Parse(to), Yesterday, Today, _options);

        Assert.Contains(expected, error);
    }

    [Fact]
    public void Custom_EndAfterLastAllowedDay_IsRejected()
    {
        var (_, _, error) = TrainingPeriodRange.Resolve(TrainingPeriod.Custom, new DateTime(2026, 6, 1), new DateTime(2026, 11, 1), new DateTime(2026, 10, 20), Today, _options);
        Assert.Contains("no puede ser posterior al 20/10/2026", error);
    }

    [Fact]
    public void UndefinedOption_IsRejected()
    {
        var (_, _, error) = TrainingPeriodRange.Resolve((TrainingPeriod)42, null, null, Yesterday, Today, _options);
        Assert.Contains("historial de entrenamiento válido", error);
    }
}

public class DemandDataRulesTests
{
    private readonly DemandForecastOptions _options = new();

    private static DemandSeriesDto Series(int days, int daysWithSales, int unknown = 0, int invalid = 0)
    {
        var series = new DemandSeriesDto { UnknownDayCount = unknown, InvalidRecordCount = invalid };
        for (var i = 0; i < days; i++)
            series.Points.Add(new DailyDemandPointDto { Date = new DateTime(2026, 1, 1).AddDays(i), Quantity = i < daysWithSales ? 1 : 0 });
        series.StartDate = series.Points.FirstOrDefault()?.Date;
        series.EndDate = series.Points.LastOrDefault()?.Date;
        return series;
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 365, DemandDataStatus.NoHistory, "No hay información")]
    [InlineData(120, 0, 0, 0, 365, DemandDataStatus.NoHistory, "no tuvo ventas")]
    [InlineData(31, 31, 0, 0, 31, DemandDataStatus.InsufficientHistory, "demasiado corto")]
    [InlineData(40, 40, 0, 0, 365, DemandDataStatus.InsufficientHistory, "Solo hay información")]
    [InlineData(120, 5, 0, 0, 365, DemandDataStatus.InsufficientHistory, "no contiene suficientes datos")]
    [InlineData(120, 120, 3, 0, 365, DemandDataStatus.InvalidData, "días sin información")]
    [InlineData(120, 120, 0, 1, 365, DemandDataStatus.InvalidData, "cantidades inválidas")]
    [InlineData(60, 60, 0, 0, 365, DemandDataStatus.ForecastOnly, "no alcanza para evaluar")]
    [InlineData(70, 70, 0, 0, 365, DemandDataStatus.Evaluable, "suficiente")]
    public void ForTraining_ReturnsExpectedStatus(int days, int withSales, int unknown, int invalid, int requested, DemandDataStatus expected, string message)
    {
        var availability = DemandDataRules.ForTraining(Series(days, withSales, unknown, invalid), _options, horizon: 14, requestedDays: requested, evaluationDays: 14);

        Assert.Equal(expected, availability.Status);
        Assert.Contains(message, availability.Message);
        Assert.Equal(expected is DemandDataStatus.ForecastOnly or DemandDataStatus.Evaluable, availability.CanForecast);
        Assert.Equal(expected == DemandDataStatus.Evaluable, availability.CanEvaluate);
    }

    [Theory]
    [InlineData(1, 56)]
    [InlineData(14, 56)]
    [InlineData(30, 60)]
    [InlineData(60, 120)]
    public void RequiredDays_DependOnHorizon(int horizon, int expected)
    {
        Assert.Equal(expected, _options.RequiredTrainingDaysFor(horizon));
    }

    [Fact]
    public void LongHorizon_NeedsLongerHistory()
    {
        // 3 meses (92 días) alcanzan para 14 días, pero no para 60 (se necesitan 120).
        var series = Series(92, 92);

        Assert.True(DemandDataRules.ForTraining(series, _options, 14, 92, 14).CanForecast);
        var sixty = DemandDataRules.ForTraining(series, _options, 60, 92, 14);
        Assert.False(sixty.CanForecast);
        Assert.Contains("120", sixty.Message);
    }

    [Theory]
    [InlineData(90, 7)]
    [InlineData(179, 7)]
    [InlineData(180, 14)]
    [InlineData(1000, 14)]
    public void WindowSize_DependsOnTrainingDays(int trainingDays, int expectedWindow)
    {
        Assert.Equal(expectedWindow, _options.WindowSizeFor(trainingDays));
        Assert.True(_options.SeriesLengthFor(expectedWindow) >= 2 * expectedWindow);
    }

    [Fact]
    public void Options_Validate_RejectsIncompatibleValues()
    {
        Assert.Throws<InvalidOperationException>(() => new DemandForecastOptions { WindowSize = 1 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new DemandForecastOptions { MinimumTrainingDays = 10 }.Validate());
        Assert.Throws<InvalidOperationException>(() => new DemandForecastOptions { ModerateSalesMinShare = 0.7 }.Validate());
        new DemandForecastOptions().Validate();
    }
}

public class DemandProfileRulesTests
{
    private readonly DemandForecastOptions _options = new();

    private static List<DailyDemandPointDto> Points(int days, Func<int, double> quantity)
        => Enumerable.Range(0, days).Select(i => new DailyDemandPointDto { Date = new DateTime(2026, 1, 1).AddDays(i), Quantity = quantity(i) }).ToList();

    [Theory]
    [InlineData(1, DemandBehavior.Frequent)]      // todos los días
    [InlineData(2, DemandBehavior.Moderate)]      // 50 % de los días
    [InlineData(10, DemandBehavior.Sporadic)]     // 10 % de los días
    public void Classify_UsesShareOfDaysWithSales(int every, DemandBehavior expected)
    {
        var profile = DemandProfileRules.Classify(Points(100, i => i % every == 0 ? 2 : 0), _options, hasEnoughHistory: true);

        Assert.Equal(expected, profile.Behavior);
        Assert.Equal(100, profile.ObservedDays);
        Assert.Equal(Math.Round(100.0 - 100.0 / every, 1), profile.ZeroDayPercent, 1);
    }

    [Fact]
    public void Classify_WithoutEnoughHistory_IsInsufficient()
    {
        var profile = DemandProfileRules.Classify(Points(20, _ => 3), _options, hasEnoughHistory: false);
        Assert.Equal(DemandBehavior.InsufficientHistory, profile.Behavior);
        Assert.Equal(60, profile.TotalQuantity);
    }

    [Fact]
    public void Classify_DetectsProductThatStoppedSelling()
    {
        var profile = DemandProfileRules.Classify(Points(100, i => i < 60 ? 2 : 0), _options, hasEnoughHistory: true);

        Assert.Equal(40, profile.TrailingDaysWithoutSales);
        Assert.True(profile.StoppedSelling);
        Assert.Equal(1.2, profile.AverageDailyQuantity, 2);
        Assert.Equal(2, profile.AverageQuantityWhenSold, 2);
    }
}

public class TrainingPeriodServiceTests
{
    private static readonly DateTime Today = new(2026, 11, 6);

    private readonly InMemoryDatabase _db = new();
    private readonly Product _product;
    private readonly FakeForecastEngine _engine = new();
    private readonly DemandForecastOptions _options = new();
    private readonly DemandForecastService _service;
    private readonly DemandForecastEvaluationService _evaluation;

    public TrainingPeriodServiceTests()
    {
        var category = _db.AddCategory("Abarrotes");
        var supplier = _db.AddSupplier("Proveedor");
        _product = _db.AddProduct("ALI-0001", category, supplier, currentStock: 50);
        var clock = new FixedTimeProvider(Today);
        var products = new FakeProductRepository(_db);
        var history = new DemandHistoryService(new FakeSaleRepository(_db), new FakeSyntheticSalesRepository(_db), products, clock);
        _service = new DemandForecastService(history, products, _engine, _options, clock);
        _evaluation = new DemandForecastEvaluationService(history, products, _engine, _options, clock);
    }

    /// <summary>
    /// Ventas diarias que terminan ayer; la cantidad depende de qué tan lejos está el día.
    /// </summary>
    private void AddDailySales(int days, Func<int, int>? quantity = null)
    {
        for (var i = days; i >= 1; i--)
        {
            var q = quantity?.Invoke(i) ?? i % 5 + 1;
            if (q > 0)
                _db.AddSale(Today.AddDays(-i).AddHours(10), _product, q);
        }
    }

    private DemandForecastRequestDto Forecast(TrainingPeriod period, int horizon = 14, DateTime? from = null, DateTime? to = null)
        => new() { ProductId = _product.Id, DataSource = SalesDataSource.Real, ForecastHorizon = horizon, TrainingPeriod = period, StartDate = from, EndDate = to };

    private DemandForecastEvaluationRequestDto Evaluation(TrainingPeriod period, int days = 14, DateTime? from = null, DateTime? to = null)
        => new() { ProductId = _product.Id, DataSource = SalesDataSource.Real, EvaluationHorizon = days, TrainingPeriod = period, StartDate = from, EndDate = to };

    [Theory]
    [InlineData(TrainingPeriod.Last3Months, 92)]
    [InlineData(TrainingPeriod.Last6Months, 184)]
    [InlineData(TrainingPeriod.Last12Months, 365)]
    [InlineData(TrainingPeriod.AllHistory, 700)]
    public async Task Forecast_TrainsOnlyWithTheSelectedPeriod(TrainingPeriod period, int expectedDays)
    {
        AddDailySales(700);

        var result = await _service.ForecastAsync(Forecast(period));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(expectedDays, _engine.LastTrainingSeries!.Count);
        Assert.Equal(expectedDays, result.TrainingObservationCount);
        Assert.Equal(Today.AddDays(-1), result.HistoricalEndDate);
        Assert.Equal(Today.AddDays(-expectedDays), result.HistoricalStartDate);
        Assert.Equal(period, result.TrainingPeriod);
    }

    [Fact]
    public async Task Forecast_LastMonth_IsTooShort_AndModelIsNotTrained()
    {
        AddDailySales(700);

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.LastMonth));

        Assert.False(result.IsSuccessful);
        Assert.Contains("demasiado corto para generar una predicción confiable", result.ErrorMessage);
        Assert.Contains("Prueba con los últimos 3 o 6 meses", result.ErrorMessage);
        Assert.Null(_engine.LastTrainingSeries);
    }

    [Fact]
    public async Task Forecast_LongHorizonWithShortPeriod_IsRejected()
    {
        AddDailySales(700);

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Last3Months, horizon: 60));

        Assert.False(result.IsSuccessful);
        Assert.Contains("al menos 120 días", result.ErrorMessage);
    }

    [Fact]
    public async Task Forecast_Custom_UsesExactDates()
    {
        AddDailySales(700);
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 9, 30);

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Custom, from: from, to: to));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal((to - from).Days + 1, _engine.LastTrainingSeries!.Count);
        Assert.Equal(from, result.HistoricalStartDate);
        Assert.Equal(to, result.HistoricalEndDate);
        Assert.Equal(Today.AddDays(1), result.ForecastStartDate);   // los días entre el 30/09 y hoy se calculan, pero no se muestran
    }

    [Fact]
    public async Task Forecast_Custom_WithFutureDates_IsRejected()
    {
        AddDailySales(200);

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Custom, from: Today.AddDays(-90), to: Today.AddDays(3)));

        Assert.False(result.IsSuccessful);
        Assert.Contains("no puede incluir el día de hoy ni fechas futuras", result.ErrorMessage);
    }

    [Fact]
    public async Task Forecast_Custom_EndingTooLongAgo_IsRejected()
    {
        AddDailySales(700);

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Custom, from: new DateTime(2026, 1, 1), to: new DateTime(2026, 6, 30)));

        Assert.False(result.IsSuccessful);
        Assert.Contains("demasiado lejos de hoy", result.ErrorMessage);
    }

    [Fact]
    public async Task Forecast_HistoryStartsAfterSelectedPeriod_ShowsRealRangeUsed()
    {
        AddDailySales(100);   // el producto se vende desde hace 100 días

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Last12Months));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(100, result.TrainingObservationCount);
        Assert.Equal(Today.AddDays(-100), result.HistoricalStartDate);
        Assert.Equal(Today.AddDays(-365), result.RequestedStartDate);   // los días anteriores no se rellenan con 0
    }

    [Fact]
    public async Task Forecast_IntermittentProduct_IsClassifiedAsSporadic()
    {
        AddDailySales(200, i => i % 10 == 0 ? 2 : 0);   // vende 1 de cada 10 días

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Last6Months));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(DemandBehavior.Sporadic, result.Profile!.Behavior);
        Assert.InRange(result.Profile.ZeroDayPercent, 85, 95);
    }

    [Fact]
    public async Task Forecast_AllZero_IsMarkedButStillShown()
    {
        AddDailySales(200);
        _engine.Behavior = (_, h) => new ForecastEngineResult { Forecast = Enumerable.Repeat(-0.4f, h).ToArray() };

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Last6Months));

        Assert.True(result.IsSuccessful);
        Assert.True(result.IsZeroForecast);
        Assert.All(result.ForecastPeriods, p => Assert.Equal(0, p.PredictedQuantity));
    }

    [Fact]
    public async Task Forecast_SmallPositiveValues_AreKept()
    {
        AddDailySales(200);
        _engine.Behavior = (_, h) => new ForecastEngineResult { Forecast = Enumerable.Repeat(0.15f, h).ToArray() };

        var result = await _service.ForecastAsync(Forecast(TrainingPeriod.Last6Months));

        Assert.False(result.IsZeroForecast);
        Assert.All(result.ForecastPeriods, p => Assert.Equal(0.15, p.PredictedQuantity, 2));
        Assert.Equal(2.1, result.TotalPredictedQuantity, 2);   // 14 × 0,15
    }

    [Theory]
    [InlineData(TrainingPeriod.Last3Months, 92)]     // 23/07/2026 – 22/10/2026
    [InlineData(TrainingPeriod.Last6Months, 183)]    // 23/04/2026 – 22/10/2026
    [InlineData(TrainingPeriod.Last12Months, 365)]
    public async Task Evaluate_TrainingPeriod_EndsBeforeEvaluatedDays(TrainingPeriod period, int expectedDays)
    {
        AddDailySales(700);

        var result = await _evaluation.EvaluateAsync(Evaluation(period, days: 14));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(Today.AddDays(-1), result.EvaluationEndDate);
        Assert.Equal(Today.AddDays(-14), result.EvaluationStartDate);
        Assert.Equal(Today.AddDays(-15), result.TrainingEndDate);
        Assert.Equal(expectedDays, result.TrainingObservationCount);
        Assert.Equal(expectedDays, _engine.LastTrainingSeries!.Count);   // el modelo no ve los días evaluados
        Assert.Equal(14, _engine.LastHorizon);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(14)]
    [InlineData(30)]
    public async Task Evaluate_Horizons_CompareSameDates(int days)
    {
        AddDailySales(400);

        var result = await _evaluation.EvaluateAsync(Evaluation(TrainingPeriod.Last6Months, days));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(days, result.EvaluationPeriods.Count);
        Assert.Equal(result.EvaluationPeriods.Sum(p => p.ActualQuantity), result.TotalActualQuantity);
        Assert.Equal(result.EvaluationPeriods.Sum(p => p.BaselineQuantity), result.TotalBaselineQuantity);
    }

    [Fact]
    public async Task Evaluate_Custom_EvaluatesTheDaysAfterTheSelectedEnd()
    {
        AddDailySales(400);
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 6, 30);

        var result = await _evaluation.EvaluateAsync(Evaluation(TrainingPeriod.Custom, 14, from, to));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(from, result.TrainingStartDate);
        Assert.Equal(to, result.TrainingEndDate);
        Assert.Equal(new DateTime(2026, 7, 1), result.EvaluationStartDate);
        Assert.Equal(new DateTime(2026, 7, 14), result.EvaluationEndDate);
    }

    [Fact]
    public async Task Evaluate_Custom_EndTooCloseToToday_IsRejected()
    {
        AddDailySales(400);

        var result = await _evaluation.EvaluateAsync(Evaluation(TrainingPeriod.Custom, 14, new DateTime(2026, 5, 1), Today.AddDays(-5)));

        Assert.False(result.IsSuccessful);
        Assert.Contains("como máximo el 22/10/2026", result.ErrorMessage);
    }

    [Fact]
    public async Task Evaluate_ZeroForecastWithSales_IsUnfavorable()
    {
        AddDailySales(400);
        _engine.Behavior = (_, h) => new ForecastEngineResult { Forecast = new float[h] };

        var result = await _evaluation.EvaluateAsync(Evaluation(TrainingPeriod.Last6Months));

        Assert.True(result.IsSuccessful);
        Assert.True(result.IsZeroForecast);
        Assert.True(result.IsUnfavorable);
        Assert.Equal(100, result.WAPE);
    }

    [Fact]
    public async Task Evaluate_SameEstimateAsReference_IsReportedAsTie()
    {
        AddDailySales(400);
        // El motor repite el último valor del entrenamiento, igual que la referencia.
        _engine.Behavior = (t, h) => new ForecastEngineResult { Forecast = Enumerable.Repeat(t[^1], h).ToArray() };

        var result = await _evaluation.EvaluateAsync(Evaluation(TrainingPeriod.Last6Months));

        Assert.Equal(ModelComparison.Equal, result.Comparison);
        Assert.Equal(result.MAE, result.BaselineMAE);
        Assert.True(result.IsUnfavorable);   // un empate no se presenta como mejora
    }

    [Fact]
    public async Task Evaluate_BetterThanReference_IsNotUnfavorable()
    {
        AddDailySales(400, i => 3);   // demanda constante: el modelo acierta exactamente
        _engine.Behavior = (_, h) => new ForecastEngineResult { Forecast = Enumerable.Repeat(3f, h).ToArray() };

        var result = await _evaluation.EvaluateAsync(Evaluation(TrainingPeriod.Last6Months));

        // Con demanda constante la referencia también acierta: se informa empate, no mejora.
        Assert.Equal(ModelComparison.Equal, result.Comparison);
        Assert.Equal(0, result.MAE);
    }
}
