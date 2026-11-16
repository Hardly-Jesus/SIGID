using SIGID.Application.DTOs.Demand;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Forecasting;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;

namespace SIGID.Application.Tests.Fakes;

/// <summary>
/// Repositorio en memoria de los datos de prueba (HU-17).
/// </summary>
public class FakeSyntheticSalesRepository : ISyntheticSalesRepository
{
    private readonly InMemoryDatabase _db;

    public FakeSyntheticSalesRepository(InMemoryDatabase db) => _db = db;

    /// <summary>
    /// Si se activa, simula un error al guardar.
    /// </summary>
    public bool FailOnSave { get; set; }

    public Task AddBatchAsync(SyntheticSalesBatch batch, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            if (FailOnSave)
                throw new BusinessException("No se pudieron guardar los datos de prueba. No se guardó ningún registro; inténtalo nuevamente.");

            batch.Id = _db.NextId();
            foreach (var record in batch.Records)
            {
                record.Id = _db.NextId();
                record.BatchId = batch.Id;
            }
            _db.SyntheticBatches.Add(batch);
        }
        return Task.CompletedTask;
    }

    public Task<SyntheticSalesBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.SyntheticBatches.FirstOrDefault(b => b.Id == batchId));
    }

    public Task<IReadOnlyList<SyntheticSalesBatch>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IReadOnlyList<SyntheticSalesBatch> result = _db.SyntheticBatches.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id).ToList();
            return Task.FromResult(result);
        }
    }

    public Task<SyntheticSalesBatch?> GetBatchByKeyAsync(string parametersKey, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.SyntheticBatches.FirstOrDefault(b => b.ParametersKey == parametersKey));
    }

    public Task<IReadOnlyList<int>> GetProductsWithDataAsync(IReadOnlyCollection<int> productIds, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            var used = _db.SyntheticBatches
                .Where(b => b.StartDate <= toDate.Date && b.EndDate >= fromDate.Date)
                .SelectMany(b => b.ProductIds.Split(',').Select(int.Parse))
                .ToHashSet();
            IReadOnlyList<int> result = productIds.Where(used.Contains).Distinct().ToList();
            return Task.FromResult(result);
        }
    }

    public Task<bool> DeleteBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
            return Task.FromResult(_db.SyntheticBatches.RemoveAll(b => b.Id == batchId) > 0);
    }

    public Task<IReadOnlyList<SyntheticSalesRecord>> GetRecordsAsync(int? productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IReadOnlyList<SyntheticSalesRecord> result = AllRecords()
                .Where(r => r.Date >= fromDate.Date && r.Date <= toDate.Date && (productId == null || r.ProductId == productId))
                .OrderBy(r => r.Date).ThenBy(r => r.ProductId)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyDictionary<DateTime, int>> GetDailyQuantitiesAsync(int productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            IReadOnlyDictionary<DateTime, int> result = AllRecords()
                .Where(r => r.ProductId == productId && r.Date >= fromDate.Date && r.Date <= toDate.Date)
                .GroupBy(r => r.Date)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
            return Task.FromResult(result);
        }
    }

    public Task<IReadOnlyList<(DateTime StartDate, DateTime EndDate)>> GetCoverageAsync(int productId, CancellationToken cancellationToken = default)
    {
        lock (_db.Sync)
        {
            var id = productId.ToString();
            IReadOnlyList<(DateTime, DateTime)> result = _db.SyntheticBatches
                .Where(b => b.ProductIds.Split(',').Contains(id))
                .OrderBy(b => b.StartDate)
                .Select(b => (b.StartDate, b.EndDate))
                .ToList();
            return Task.FromResult(result);
        }
    }

    private IEnumerable<SyntheticSalesRecord> AllRecords() => _db.SyntheticBatches.SelectMany(b => b.Records);
}

/// <summary>
/// Motor de pronóstico simulado: permite probar el servicio sin depender de ML.NET.
/// </summary>
public class FakeForecastEngine : IDemandForecastEngine
{
    public Func<IReadOnlyList<float>, int, ForecastEngineResult>? Behavior { get; set; }

    public IReadOnlyList<float>? LastTrainingSeries { get; private set; }

    public int LastHorizon { get; private set; }

    public ForecastEngineResult Forecast(IReadOnlyList<float> trainingSeries, int horizon)
    {
        LastTrainingSeries = trainingSeries.ToList();
        LastHorizon = horizon;

        if (Behavior is not null)
            return Behavior(trainingSeries, horizon);

        // Por defecto repite el promedio, con un margen de ±2.
        var average = trainingSeries.Average();
        return new ForecastEngineResult
        {
            Forecast = Enumerable.Repeat(average, horizon).ToArray(),
            LowerBound = Enumerable.Repeat(average - 2, horizon).ToArray(),
            UpperBound = Enumerable.Repeat(average + 2, horizon).ToArray()
        };
    }
}

/// <summary>
/// Reloj fijo para que las pruebas no dependan de la fecha real.
/// </summary>
public class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTime now) => _now = new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
