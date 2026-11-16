using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Services.Demand;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;

namespace SIGID.Application.Tests;

public class SalesAnalysisServiceTests
{
    private static readonly DateTime Today = new(2026, 11, 6);

    private readonly InMemoryDatabase _db = new();
    private readonly Product _product;
    private readonly SalesAnalysisService _service;

    public SalesAnalysisServiceTests()
    {
        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _product = _db.AddProduct("A", category, supplier, currentStock: 10);
        var clock = new FixedTimeProvider(Today);
        var history = new DemandHistoryService(new FakeSaleRepository(_db), new FakeSyntheticSalesRepository(_db), new FakeProductRepository(_db), clock);
        _service = new SalesAnalysisService(history, new FakeProductRepository(_db), new DemandForecastOptions(), clock);
    }

    private SalesAnalysisFilterDto Filter(DateTime start, DateTime end, SalesGrouping groupBy = SalesGrouping.Day)
        => new() { ProductId = _product.Id, StartDate = start, EndDate = end, GroupBy = groupBy, DataSource = SalesDataSource.Real };

    [Fact]
    public async Task Analyze_ByDay_CalculatesIndicators()
    {
        _db.AddSale(new DateTime(2026, 11, 1), _product, 2);
        _db.AddSale(new DateTime(2026, 11, 2), _product, 6);
        _db.AddSale(new DateTime(2026, 11, 4), _product, 4);

        var result = await _service.AnalyzeAsync(Filter(new DateTime(2026, 11, 1), new DateTime(2026, 11, 5)));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(new double[] { 2, 6, 0, 4, 0 }, result.Periods.Select(p => p.TotalQuantity));
        Assert.Equal(12, result.TotalQuantity);
        Assert.Equal(2.4, result.AverageQuantity, 6);
        Assert.Equal(6, result.MaximumQuantity);
        Assert.Equal(new DateTime(2026, 11, 2), result.MaximumPeriod);
        Assert.Equal(0, result.MinimumQuantity);
        Assert.Equal(new DateTime(2026, 11, 3), result.MinimumPeriod);
        Assert.Equal(4, result.Periods[1].ChangeFromPrevious);
        Assert.Null(result.Periods[0].ChangeFromPrevious);
        Assert.Equal(10, _product.CurrentStock);   // solo consulta
    }

    [Fact]
    public void GroupPeriods_ByWeek_StartsOnMonday_AndByMonth_OnTheFirst()
    {
        var points = Enumerable.Range(0, 14)
            .Select(i => new DailyDemandPointDto { Date = new DateTime(2026, 10, 28).AddDays(i), Quantity = 1 })
            .ToList();   // del miércoles 28/10 al martes 10/11

        var weeks = SalesAnalysisService.GroupPeriods(points, SalesGrouping.Week);
        var months = SalesAnalysisService.GroupPeriods(points, SalesGrouping.Month);

        Assert.Equal(new[] { new DateTime(2026, 10, 26), new DateTime(2026, 11, 2), new DateTime(2026, 11, 9) }, weeks.Select(w => w.Period));
        Assert.Equal(new double[] { 5, 7, 2 }, weeks.Select(w => w.TotalQuantity));
        Assert.Equal(new[] { new DateTime(2026, 10, 1), new DateTime(2026, 11, 1) }, months.Select(m => m.Period));
        Assert.Equal(new double[] { 4, 10 }, months.Select(m => m.TotalQuantity));
    }

    [Theory]
    [InlineData(new double[] { 10, 12, 14, 16, 18 }, SalesTrend.Increasing)]
    [InlineData(new double[] { 18, 16, 14, 12, 10 }, SalesTrend.Decreasing)]
    [InlineData(new double[] { 10, 11, 9, 10, 10 }, SalesTrend.Stable)]
    [InlineData(new double[] { 10, 12 }, SalesTrend.NotEnoughData)]
    [InlineData(new double[] { 0, 0, 0 }, SalesTrend.NotEnoughData)]
    public void CalculateTrend_ClassifiesHistory(double[] values, SalesTrend expected)
    {
        var (trend, _) = SalesAnalysisService.CalculateTrend(values);
        Assert.Equal(expected, trend);
    }

    [Fact]
    public async Task Analyze_WithoutSales_ShowsNoticeAndNoIndicators()
    {
        var result = await _service.AnalyzeAsync(Filter(Today.AddDays(-10), Today));

        Assert.True(result.IsSuccessful);
        Assert.False(result.HasSales);
        Assert.Equal("No se encontraron datos de ventas para el período seleccionado.", result.Notice);
    }

    [Fact]
    public async Task Analyze_InvalidFilters_ReturnFriendlyMessages()
    {
        var noProduct = await _service.AnalyzeAsync(new SalesAnalysisFilterDto { StartDate = Today.AddDays(-5), EndDate = Today });
        var inverted = await _service.AnalyzeAsync(Filter(Today, Today.AddDays(-5)));
        var future = await _service.AnalyzeAsync(Filter(Today.AddDays(-5), Today.AddDays(3)));
        var missing = await _service.AnalyzeAsync(new SalesAnalysisFilterDto { ProductId = 9999, StartDate = Today.AddDays(-5), EndDate = Today });

        Assert.Equal("Selecciona un producto para continuar.", noProduct.ErrorMessage);
        Assert.Contains("no puede ser posterior", inverted.ErrorMessage);
        Assert.Contains("fechas futuras", future.ErrorMessage);
        Assert.Equal("El producto seleccionado no existe.", missing.ErrorMessage);
    }
}

public class SalesAnalysisPartialPeriodTests
{
    [Fact]
    public void IncompleteWeeks_AreMarked_AndNotUsedForIndicators()
    {
        // Del sábado 08/08/2026 al miércoles 26/08/2026: semana inicial (2 días) y final (3 días) incompletas.
        var points = Enumerable.Range(0, 19)
            .Select(i => new DailyDemandPointDto { Date = new DateTime(2026, 8, 8).AddDays(i), Quantity = 2 })
            .ToList();

        var result = new SalesAnalysisResultDto { GroupBy = SalesGrouping.Week, Periods = SalesAnalysisService.GroupPeriods(points, SalesGrouping.Week) };
        SalesAnalysisService.CalculateIndicators(result);

        Assert.Equal(new[] { true, false, false, true }, result.Periods.Select(p => p.IsPartial));
        Assert.Equal(new[] { 2, 7, 7, 3 }, result.Periods.Select(p => p.DayCount));
        Assert.Equal(38, result.TotalQuantity);            // el total sí incluye todo
        Assert.Equal(2, result.IndicatorPeriodCount);
        Assert.Equal(14, result.AverageQuantity);          // solo semanas completas
        Assert.Equal(14, result.MinimumQuantity);
        Assert.Equal(SalesTrend.NotEnoughData, result.Trend);   // 2 semanas completas no alcanzan para la tendencia
    }
}
