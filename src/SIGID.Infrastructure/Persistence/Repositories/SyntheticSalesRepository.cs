using Microsoft.EntityFrameworkCore;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Repositories;

public class SyntheticSalesRepository : ISyntheticSalesRepository
{
    private readonly SigidDbContext _context;

    public SyntheticSalesRepository(SigidDbContext context)
    {
        _context = context;
    }

    public async Task AddBatchAsync(SyntheticSalesBatch batch, CancellationToken cancellationToken = default)
    {
        // Un solo SaveChanges: EF guarda el lote y sus registros dentro de una transacción.
        _context.SyntheticSalesBatches.Add(batch);
        await PersistenceErrors.SaveChangesAsync(_context,
            "No se pudieron guardar los datos de prueba. No se guardó ningún registro; inténtalo nuevamente.",
            cancellationToken);
    }

    public async Task<SyntheticSalesBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await _context.SyntheticSalesBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
    }

    public async Task<IReadOnlyList<SyntheticSalesBatch>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SyntheticSalesBatches
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<SyntheticSalesBatch?> GetBatchByKeyAsync(string parametersKey, CancellationToken cancellationToken = default)
    {
        return await _context.SyntheticSalesBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ParametersKey == parametersKey, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetProductsWithDataAsync(IReadOnlyCollection<int> productIds, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // Se revisan los lotes (no solo los registros): un producto puede estar en un lote
        // aunque todos sus días hayan quedado en 0.
        var overlapping = await _context.SyntheticSalesBatches
            .AsNoTracking()
            .Where(b => b.StartDate <= toDate.Date && b.EndDate >= fromDate.Date)
            .Select(b => b.ProductIds)
            .ToListAsync(cancellationToken);

        var usedIds = overlapping
            .SelectMany(ids => ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(int.Parse)
            .ToHashSet();

        return productIds.Where(usedIds.Contains).Distinct().ToList();
    }

    public async Task<bool> DeleteBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var batch = await _context.SyntheticSalesBatches.FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
        if (batch is null)
            return false;

        // Los registros del lote se eliminan en cascada.
        _context.SyntheticSalesBatches.Remove(batch);
        await PersistenceErrors.SaveChangesAsync(_context, "No se pudo eliminar el lote de datos de prueba.", cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<SyntheticSalesRecord>> GetRecordsAsync(int? productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.SyntheticSalesRecords
            .AsNoTracking()
            .Where(r => r.Date >= fromDate.Date && r.Date <= toDate.Date);

        if (productId.HasValue)
            query = query.Where(r => r.ProductId == productId.Value);

        return await query
            .OrderBy(r => r.Date)
            .ThenBy(r => r.ProductId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> GetDailyQuantitiesAsync(int productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SyntheticSalesRecords
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.Date >= fromDate.Date && r.Date <= toDate.Date)
            .GroupBy(r => r.Date)
            .Select(g => new { Day = g.Key, Quantity = g.Sum(r => r.Quantity) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Day, r => r.Quantity);
    }

    public async Task<IReadOnlyList<(DateTime StartDate, DateTime EndDate)>> GetCoverageAsync(int productId, CancellationToken cancellationToken = default)
    {
        // Lotes cuyo listado de productos incluye este producto.
        var batches = await _context.SyntheticSalesBatches
            .AsNoTracking()
            .Select(b => new { b.StartDate, b.EndDate, b.ProductIds })
            .ToListAsync(cancellationToken);

        var id = productId.ToString();
        return batches
            .Where(b => b.ProductIds.Split(',').Contains(id))
            .OrderBy(b => b.StartDate)
            .Select(b => (b.StartDate, b.EndDate))
            .ToList();
    }
}
