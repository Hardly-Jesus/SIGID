using SIGID.Application.DTOs.Demand;
using SIGID.Application.Exceptions;
using SIGID.Application.Services.Demand;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Tests;

public class DemandHistoryServiceTests
{
    private static readonly DateTime Today = new(2026, 11, 6);

    private readonly InMemoryDatabase _db = new();
    private readonly Product _product;
    private readonly Product _otherProduct;
    private readonly DemandHistoryService _service;

    public DemandHistoryServiceTests()
    {
        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _product = _db.AddProduct("A", category, supplier);
        _otherProduct = _db.AddProduct("B", category, supplier);
        _service = new DemandHistoryService(new FakeSaleRepository(_db), new FakeSyntheticSalesRepository(_db),
            new FakeProductRepository(_db), new FixedTimeProvider(Today));
    }

    [Fact]
    public async Task RealSeries_FillsDaysWithoutSalesWithZero_AndGroupsSameDay()
    {
        _db.AddSale(new DateTime(2026, 11, 1, 9, 0, 0), _product, 2);
        _db.AddSale(new DateTime(2026, 11, 1, 18, 30, 0), _product, 3);   // mismo día: se suma
        _db.AddSale(new DateTime(2026, 11, 4, 10, 0, 0), _product, 1);
        _db.AddSale(new DateTime(2026, 11, 2, 10, 0, 0), _otherProduct, 50);   // otro producto: no cuenta

        var series = await _service.GetDailySeriesAsync(_product.Id, SalesDataSource.Real, new DateTime(2026, 10, 1), new DateTime(2026, 11, 5));

        // Antes de la primera venta de SIGID no hay información: la serie empieza ese día.
        Assert.Equal(new DateTime(2026, 11, 1), series.StartDate);
        Assert.Equal(new DateTime(2026, 11, 5), series.EndDate);
        Assert.Equal(new double[] { 5, 0, 0, 1, 0 }, series.Points.Select(p => p.Quantity));
        Assert.Equal(2, series.DaysWithSales);
        Assert.Equal(0, series.UnknownDayCount);
    }

    [Fact]
    public async Task RealSeries_WithoutSales_IsEmpty()
    {
        var series = await _service.GetDailySeriesAsync(_product.Id, SalesDataSource.Real, Today.AddDays(-30), Today);

        Assert.Empty(series.Points);
        Assert.Null(series.StartDate);
    }

    [Fact]
    public async Task SyntheticSeries_DoesNotMixWithRealSales()
    {
        _db.AddSale(new DateTime(2026, 10, 2), _product, 100);
        AddBatch(new DateTime(2026, 10, 1), new DateTime(2026, 10, 3), (new DateTime(2026, 10, 1), 4), (new DateTime(2026, 10, 3), 6));

        var synthetic = await _service.GetDailySeriesAsync(_product.Id, SalesDataSource.Synthetic, new DateTime(2026, 10, 1), new DateTime(2026, 10, 3));
        var real = await _service.GetDailySeriesAsync(_product.Id, SalesDataSource.Real, new DateTime(2026, 10, 1), new DateTime(2026, 10, 3));

        Assert.Equal(new double[] { 4, 0, 6 }, synthetic.Points.Select(p => p.Quantity));
        Assert.Equal(new double[] { 100, 0 }, real.Points.Select(p => p.Quantity));
    }

    [Fact]
    public async Task SyntheticSeries_GapBetweenBatches_IsUnknownNotZero()
    {
        AddBatch(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), (new DateTime(2026, 1, 5), 3));
        AddBatch(new DateTime(2026, 1, 21), new DateTime(2026, 1, 31), (new DateTime(2026, 1, 25), 2));

        var series = await _service.GetDailySeriesAsync(_product.Id, SalesDataSource.Synthetic, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Equal(21, series.Points.Count);
        Assert.Equal(10, series.UnknownDayCount);
        Assert.DoesNotContain(series.Points, p => p.Date > new DateTime(2026, 1, 10) && p.Date < new DateTime(2026, 1, 21));
    }

    [Fact]
    public async Task Series_IsOrderedAndHasNoDuplicateDays()
    {
        AddBatch(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), (new DateTime(2026, 3, 15), 2));

        var series = await _service.GetDailySeriesAsync(_product.Id, SalesDataSource.Synthetic, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        Assert.Equal(31, series.Points.Count);
        for (var i = 1; i < series.Points.Count; i++)
            Assert.Equal(series.Points[i - 1].Date.AddDays(1), series.Points[i].Date);
    }

    [Fact]
    public async Task MissingProduct_Throws()
    {
        await Assert.ThrowsAsync<BusinessException>(() =>
            _service.GetDailySeriesAsync(9999, SalesDataSource.Real, Today.AddDays(-5), Today));
    }

    private void AddBatch(DateTime start, DateTime end, params (DateTime Date, int Quantity)[] records)
    {
        var batch = new SyntheticSalesBatch
        {
            Id = _db.NextId(),
            StartDate = start,
            EndDate = end,
            Pattern = DemandPattern.Stable,
            ProductIds = _product.Id.ToString(),
            ProductCount = 1,
            ParametersKey = Guid.NewGuid().ToString()
        };
        foreach (var (date, quantity) in records)
            batch.Records.Add(new SyntheticSalesRecord { ProductId = _product.Id, Date = date, Quantity = quantity });
        batch.RecordCount = batch.Records.Count;
        _db.SyntheticBatches.Add(batch);
    }
}
