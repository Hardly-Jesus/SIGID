using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Services.Demand;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Tests;

public class DemandForecastServiceTests
{
    private static readonly DateTime Today = new(2026, 11, 6);

    private readonly InMemoryDatabase _db = new();
    private readonly Product _product;
    private readonly FakeForecastEngine _engine = new();
    private readonly DemandForecastOptions _options = new();
    private readonly DemandForecastService _service;
    private readonly DemandForecastEvaluationService _evaluation;

    public DemandForecastServiceTests()
    {
        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _product = _db.AddProduct("A", category, supplier, currentStock: 30);
        var clock = new FixedTimeProvider(Today);
        var products = new FakeProductRepository(_db);
        var history = new DemandHistoryService(new FakeSaleRepository(_db), new FakeSyntheticSalesRepository(_db), products, clock);
        _service = new DemandForecastService(history, products, _engine, _options, clock);
        _evaluation = new DemandForecastEvaluationService(history, products, _engine, _options, clock);
    }

    /// <summary>
    /// Ventas reales diarias de 1 a 5 unidades que terminan ayer.
    /// </summary>
    private void AddDailySales(int days)
    {
        for (var i = days; i >= 1; i--)
            _db.AddSale(Today.AddDays(-i).AddHours(10), _product, i % 5 + 1);
    }

    private DemandForecastRequestDto Request(int? horizon = 14)
        => new() { ProductId = _product.Id, DataSource = SalesDataSource.Real, ForecastHorizon = horizon };

    [Fact]
    public async Task Forecast_StartsTomorrow_AndUsesOnlyPastDays()
    {
        AddDailySales(60);

        var result = await _service.ForecastAsync(Request(14));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(14, result.ForecastPeriods.Count);
        Assert.Equal(Today.AddDays(1), result.ForecastStartDate);
        Assert.Equal(Today.AddDays(14), result.ForecastEndDate);
        Assert.Equal(Today.AddDays(-1), result.HistoricalEndDate);
        Assert.Equal(_engine.LastTrainingSeries!.Count, result.TrainingObservationCount);
        Assert.Equal(15, _engine.LastHorizon);   // hoy se calcula pero no se muestra como futuro
        Assert.True(result.HasPredictionBounds);
        Assert.Equal(30, _product.CurrentStock);
    }

    [Fact]
    public async Task Forecast_TodaysSalesAreNotUsed()
    {
        AddDailySales(60);
        _db.AddSale(Today.AddHours(9), _product, 500);

        var result = await _service.ForecastAsync(Request());

        Assert.True(result.IsSuccessful);
        Assert.DoesNotContain(500f, _engine.LastTrainingSeries!);
    }

    [Fact]
    public async Task Forecast_NegativeValuesBecomeZero()
    {
        AddDailySales(60);
        _engine.Behavior = (_, h) => new ForecastEngineResult
        {
            Forecast = Enumerable.Repeat(-3f, h).ToArray(),
            LowerBound = Enumerable.Repeat(-6f, h).ToArray(),
            UpperBound = Enumerable.Repeat(1f, h).ToArray()
        };

        var result = await _service.ForecastAsync(Request());

        Assert.True(result.IsSuccessful);
        Assert.All(result.ForecastPeriods, p =>
        {
            Assert.Equal(0, p.PredictedQuantity);
            Assert.Equal(0, p.LowerBound);
            Assert.True(p.UpperBound >= p.PredictedQuantity);
        });
    }

    [Fact]
    public async Task Forecast_InvalidModelOutput_IsTechnicalError()
    {
        AddDailySales(60);
        _engine.Behavior = (_, h) => new ForecastEngineResult { Forecast = Enumerable.Repeat(float.NaN, h).ToArray() };

        var result = await _service.ForecastAsync(Request());

        Assert.False(result.IsSuccessful);
        Assert.True(result.IsTechnicalError);
        Assert.Equal(DemandForecastService.TechnicalErrorMessage, result.ErrorMessage);
        Assert.Empty(result.ForecastPeriods);
    }

    [Fact]
    public async Task Forecast_EngineException_ShowsFriendlyMessage()
    {
        AddDailySales(60);
        _engine.Behavior = (_, _) => throw new InvalidOperationException("detalle interno");

        var result = await _service.ForecastAsync(Request());

        Assert.False(result.IsSuccessful);
        Assert.DoesNotContain("detalle interno", result.ErrorMessage);
    }

    [Fact]
    public async Task Forecast_InsufficientHistory_IsNotShown()
    {
        AddDailySales(10);

        var result = await _service.ForecastAsync(Request());

        Assert.False(result.IsSuccessful);
        Assert.Equal(DemandDataStatus.InsufficientHistory, result.Availability!.Status);
        Assert.Contains("Solo hay información de ventas de este producto desde", result.ErrorMessage);
        Assert.Null(_engine.LastTrainingSeries);   // el modelo no se ejecutó
    }

    [Fact]
    public async Task Forecast_NoHistory()
    {
        var result = await _service.ForecastAsync(Request());

        Assert.False(result.IsSuccessful);
        Assert.Contains("No hay información de ventas", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public async Task Forecast_InvalidHorizon_IsRejected(int horizon)
    {
        AddDailySales(60);

        var result = await _service.ForecastAsync(Request(horizon));

        Assert.False(result.IsSuccessful);
        Assert.Contains("período de predicción", result.ErrorMessage);
    }

    [Fact]
    public async Task Forecast_WithoutProduct_AsksToSelectOne()
    {
        var result = await _service.ForecastAsync(new DemandForecastRequestDto());
        Assert.Equal("Selecciona un producto para continuar.", result.ErrorMessage);
    }

    [Fact]
    public async Task Forecast_SyntheticData_UsesOnlySyntheticRecords()
    {
        AddDailySales(60);   // reales: no deben usarse
        var start = Today.AddDays(-90);
        var end = Today.AddDays(-1);
        var batch = new SyntheticSalesBatch
        {
            Id = _db.NextId(), StartDate = start, EndDate = end, Pattern = DemandPattern.Stable,
            ProductIds = _product.Id.ToString(), ProductCount = 1, ParametersKey = "k"
        };
        foreach (var record in SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Stable, 1))
            batch.Records.Add(record);
        _db.SyntheticBatches.Add(batch);

        var result = await _service.ForecastAsync(new DemandForecastRequestDto { ProductId = _product.Id, DataSource = SalesDataSource.Synthetic, ForecastHorizon = 7 });

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(90, _engine.LastTrainingSeries!.Count);
        Assert.Equal(start, result.HistoricalStartDate);
    }

    [Fact]
    public void BuildPeriods_WrongLength_ReturnsNull()
    {
        var output = new ForecastEngineResult { Forecast = new float[3] };
        Assert.Null(DemandForecastService.BuildPeriods(output, Today, 0, 5));
    }

    [Fact]
    public async Task Evaluate_TrainsOnlyWithDaysBeforeEvaluation()
    {
        AddDailySales(120);

        var result = await _evaluation.EvaluateAsync(new DemandForecastEvaluationRequestDto { ProductId = _product.Id, DataSource = SalesDataSource.Real, EvaluationHorizon = 14 });

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(14, result.EvaluationPeriods.Count);
        Assert.Equal(Today.AddDays(-1), result.EvaluationEndDate);
        Assert.Equal(Today.AddDays(-14), result.EvaluationStartDate);
        Assert.Equal(result.EvaluationStartDate!.Value.AddDays(-1), result.TrainingEndDate);
        Assert.Equal(result.TrainingObservationCount, _engine.LastTrainingSeries!.Count);   // sin fuga de datos futuros
        Assert.Equal(14, _engine.LastHorizon);
        Assert.Equal(120 - 14, result.TrainingObservationCount);
    }

    [Fact]
    public async Task Evaluate_MetricsMatchPeriodErrors_AndBaselineUsesLastTrainingValue()
    {
        AddDailySales(120);
        _engine.Behavior = (_, h) => new ForecastEngineResult { Forecast = Enumerable.Repeat(3f, h).ToArray() };

        var result = await _evaluation.EvaluateAsync(new DemandForecastEvaluationRequestDto { ProductId = _product.Id, DataSource = SalesDataSource.Real, EvaluationHorizon = 10 });

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        var expectedMae = result.EvaluationPeriods.Average(p => Math.Abs(p.ActualQuantity - 3));
        Assert.Equal(expectedMae, result.MAE!.Value, 4);

        // El último día de entrenamiento es el día 11 antes de hoy: 11 % 5 + 1 = 2.
        Assert.All(result.EvaluationPeriods, p => Assert.Equal(2, p.BaselineQuantity));
        Assert.NotNull(result.BaselineMAE);
        Assert.Equal(ForecastMetrics.Compare(result.MAE.Value, result.BaselineMAE!.Value), result.Comparison);
    }

    [Fact]
    public async Task Evaluate_OnlyEnoughToForecast_ReturnsForecastOnlyStatus()
    {
        AddDailySales(65);   // 65 días alcanzan para pronosticar, pero antes de los 14 evaluados quedan 51 (< 56)

        var result = await _evaluation.EvaluateAsync(new DemandForecastEvaluationRequestDto { ProductId = _product.Id, DataSource = SalesDataSource.Real, EvaluationHorizon = 14 });

        Assert.False(result.IsSuccessful);
        Assert.Equal(DemandDataStatus.ForecastOnly, result.Availability!.Status);
        Assert.Contains("evaluar la precisión", result.ErrorMessage);
    }

    [Fact]
    public async Task Evaluate_InvalidHorizon_IsRejected()
    {
        var result = await _evaluation.EvaluateAsync(new DemandForecastEvaluationRequestDto { ProductId = _product.Id, EvaluationHorizon = 3 });
        Assert.Contains("período de evaluación", result.ErrorMessage);
    }
}

