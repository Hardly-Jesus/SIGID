using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Services.Demand;
using SIGID.Application.Tests.Fakes;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Tests;

public class SyntheticSalesServiceTests
{
    private static readonly DateTime Today = new(2026, 11, 6);

    private readonly InMemoryDatabase _db = new();
    private readonly FakeSyntheticSalesRepository _repository;
    private readonly Product _product;
    private readonly Product _otherProduct;

    public SyntheticSalesServiceTests()
    {
        _repository = new FakeSyntheticSalesRepository(_db);
        var category = _db.AddCategory("Bebidas");
        var supplier = _db.AddSupplier("Proveedor");
        _product = _db.AddProduct("A", category, supplier, currentStock: 40);
        _otherProduct = _db.AddProduct("B", category, supplier, currentStock: 15);
    }

    private SyntheticSalesService CreateService(bool enabled = true, SyntheticDataOptions? options = null)
    {
        options ??= new SyntheticDataOptions();
        options.Enabled = enabled;
        return new SyntheticSalesService(_repository, new FakeProductRepository(_db), options, new FixedTimeProvider(Today));
    }

    private HistoricalSalesGenerationDto Request(int months = 12, DemandPattern pattern = DemandPattern.Stable, int seed = 42, params int[] productIds)
    {
        var end = Today.AddDays(-1);
        return new HistoricalSalesGenerationDto
        {
            StartDate = end.AddMonths(-months).AddDays(1),
            EndDate = end,
            DemandPattern = pattern,
            RandomSeed = seed,
            ProductIds = productIds.Length == 0 ? new List<int> { _product.Id } : productIds.ToList()
        };
    }

    [Fact]
    public async Task Generate_WhenDisabled_DoesNothing()
    {
        var result = await CreateService(enabled: false).GenerateAsync(Request());

        Assert.False(result.IsSuccessful);
        Assert.Contains("deshabilitada", result.ErrorMessage);
        Assert.Empty(_db.SyntheticBatches);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(24)]
    public async Task Generate_TwelveOrTwentyFourMonths_CreatesOneRecordPerDayWithSales(int months)
    {
        var request = Request(months);
        var result = await CreateService().GenerateAsync(request);

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        var batch = Assert.Single(_db.SyntheticBatches);
        var days = (request.EndDate!.Value - request.StartDate!.Value).Days + 1;

        Assert.Equal(batch.Records.Count, result.GeneratedRecords);
        Assert.InRange(batch.Records.Count, days * 0.9, days);              // casi todos los días tienen ventas
        Assert.All(batch.Records, r => Assert.True(r.Quantity > 0));
        Assert.All(batch.Records, r => Assert.InRange(r.Date, request.StartDate.Value, request.EndDate.Value));
        Assert.Equal(batch.Records.Count, batch.Records.Select(r => r.Date).Distinct().Count());   // sin días duplicados
    }

    [Fact]
    public async Task Generate_DoesNotTouchStockMovementsOrRealSales()
    {
        var sale = _db.AddSale(Today.AddDays(-3), _product, 2);

        var result = await CreateService().GenerateAsync(Request(productIds: new[] { _product.Id, _otherProduct.Id }));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(40, _product.CurrentStock);
        Assert.Equal(15, _otherProduct.CurrentStock);
        Assert.Empty(_db.Movements);
        Assert.Equal(sale, Assert.Single(_db.Sales));
        Assert.Equal(2, sale.Details.Single().Quantity);
    }

    [Fact]
    public void GenerateRecords_SameSeed_ProducesSameData_DifferentSeedChangesIt()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 12, 31);

        var first = SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Seasonal, 7);
        var second = SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Seasonal, 7);
        var other = SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Seasonal, 8);

        Assert.Equal(first.Select(r => (r.Date, r.Quantity)), second.Select(r => (r.Date, r.Quantity)));
        Assert.NotEqual(first.Select(r => (r.Date, r.Quantity)), other.Select(r => (r.Date, r.Quantity)));
    }

    [Fact]
    public void GenerateRecords_IncreasingAndDecreasing_FollowThePattern()
    {
        var start = new DateTime(2024, 11, 1);
        var end = new DateTime(2026, 10, 31);

        var increasing = SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Increasing, 1);
        var decreasing = SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Decreasing, 1);

        Assert.True(Average(increasing, start, 90) < Average(increasing, end.AddDays(-89), 90));
        Assert.True(Average(decreasing, start, 90) > Average(decreasing, end.AddDays(-89), 90));
    }

    [Fact]
    public void GenerateRecords_Stable_FirstAndLastQuarterAreSimilar()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 12, 31);
        var records = SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Stable, 3);

        var first = Average(records, start, 90);
        var last = Average(records, end.AddDays(-89), 90);
        Assert.InRange(last / first, 0.85, 1.15);
    }

    [Fact]
    public void GenerateRecords_Seasonal_HasHighAndLowMonths()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 12, 31);
        var records = SyntheticSalesService.GenerateRecords(_product, start, end, DemandPattern.Seasonal, 5);

        var monthly = records.GroupBy(r => r.Date.Month).Select(g => g.Sum(r => r.Quantity)).ToList();
        Assert.True(monthly.Max() > monthly.Min() * 1.5);
    }

    private static double Average(List<SyntheticSalesRecord> records, DateTime from, int days)
        => records.Where(r => r.Date >= from && r.Date < from.AddDays(days)).Sum(r => r.Quantity) / (double)days;

    [Fact]
    public async Task Generate_SameParametersTwice_IsRejected()
    {
        var service = CreateService();
        await service.GenerateAsync(Request());

        var second = await service.GenerateAsync(Request());

        Assert.False(second.IsSuccessful);
        Assert.Contains("mismos parámetros", second.ErrorMessage);
        Assert.Single(_db.SyntheticBatches);
    }

    [Fact]
    public async Task Generate_OverlappingPeriodForSameProduct_IsRejected()
    {
        var service = CreateService();
        await service.GenerateAsync(Request(seed: 1));

        var overlapping = await service.GenerateAsync(Request(seed: 2));

        Assert.False(overlapping.IsSuccessful);
        Assert.Contains("Ya hay datos de prueba", overlapping.ErrorMessage);
        Assert.Single(_db.SyntheticBatches);
    }

    [Fact]
    public async Task Generate_OtherProductSamePeriod_IsAllowed()
    {
        var service = CreateService();
        await service.GenerateAsync(Request(productIds: new[] { _product.Id }));

        var result = await service.GenerateAsync(Request(productIds: new[] { _otherProduct.Id }));

        Assert.True(result.IsSuccessful, result.ErrorMessage);
        Assert.Equal(2, _db.SyntheticBatches.Count);
    }

    [Fact]
    public async Task Generate_InvalidParameters_ReturnFriendlyMessages()
    {
        var service = CreateService();

        var noDates = await service.GenerateAsync(new HistoricalSalesGenerationDto { ProductIds = { _product.Id } });
        var inverted = await service.GenerateAsync(new HistoricalSalesGenerationDto { StartDate = Today.AddDays(-1), EndDate = Today.AddDays(-10), ProductIds = { _product.Id } });
        var future = await service.GenerateAsync(new HistoricalSalesGenerationDto { StartDate = Today.AddDays(-10), EndDate = Today.AddDays(1), ProductIds = { _product.Id } });
        var tooLong = await service.GenerateAsync(new HistoricalSalesGenerationDto { StartDate = Today.AddYears(-3), EndDate = Today.AddDays(-1), ProductIds = { _product.Id } });
        var noProducts = await service.GenerateAsync(new HistoricalSalesGenerationDto { StartDate = Today.AddDays(-10), EndDate = Today.AddDays(-1) });
        var missingProduct = await service.GenerateAsync(new HistoricalSalesGenerationDto { StartDate = Today.AddDays(-10), EndDate = Today.AddDays(-1), ProductIds = { 9999 } });
        var badPattern = await service.GenerateAsync(new HistoricalSalesGenerationDto { StartDate = Today.AddDays(-10), EndDate = Today.AddDays(-1), ProductIds = { _product.Id }, DemandPattern = (DemandPattern)99 });
        var badSeed = await service.GenerateAsync(new HistoricalSalesGenerationDto { StartDate = Today.AddDays(-10), EndDate = Today.AddDays(-1), ProductIds = { _product.Id }, RandomSeed = -1 });

        Assert.Contains("fecha inicial y la fecha final", noDates.ErrorMessage);
        Assert.Contains("no puede ser posterior", inverted.ErrorMessage);
        Assert.Contains("fechas futuras", future.ErrorMessage);
        Assert.Contains("no puede superar", tooLong.ErrorMessage);
        Assert.Contains("al menos un producto", noProducts.ErrorMessage);
        Assert.Contains("no existen", missingProduct.ErrorMessage);
        Assert.Contains("patrón", badPattern.ErrorMessage);
        Assert.Contains("semilla", badSeed.ErrorMessage);
        Assert.Empty(_db.SyntheticBatches);
    }

    [Fact]
    public async Task Generate_ExceedingRecordLimit_IsRejectedBeforeGenerating()
    {
        var service = CreateService(options: new SyntheticDataOptions { MaxRecordsPerBatch = 100 });

        var result = await service.GenerateAsync(Request());

        Assert.False(result.IsSuccessful);
        Assert.Contains("máximo por lote", result.ErrorMessage);
        Assert.Empty(_db.SyntheticBatches);
    }

    [Fact]
    public async Task Generate_WhenSaveFails_NothingIsStored()
    {
        _repository.FailOnSave = true;

        var result = await CreateService().GenerateAsync(Request());

        Assert.False(result.IsSuccessful);
        Assert.Contains("No se guardó ningún registro", result.ErrorMessage);
        Assert.Empty(_db.SyntheticBatches);
    }

    [Fact]
    public async Task DeleteBatch_RemovesOnlyThatBatch()
    {
        var service = CreateService();
        var first = await service.GenerateAsync(Request(productIds: new[] { _product.Id }));
        await service.GenerateAsync(Request(productIds: new[] { _otherProduct.Id }));

        Assert.True(await service.DeleteBatchAsync(first.BatchId));
        Assert.False(await service.DeleteBatchAsync(first.BatchId));

        var remaining = Assert.Single(await service.GetBatchesAsync());
        Assert.Equal(_otherProduct.Name, Assert.Single(remaining.ProductNames));
    }
}
