using Microsoft.EntityFrameworkCore;
using SIGID.Application.DTOs.Sales;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly SigidDbContext _context;

    public SaleRepository(SigidDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        // EF guarda la venta y sus detalles en el mismo SaveChanges.
        _context.Sales.Add(sale);
        await PersistenceErrors.SaveChangesAsync(_context, "No se pudo registrar la venta.", cancellationToken);
    }

    public async Task<Sale?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .AsNoTracking()
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Sale?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RequestId == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<SaleDetail>> GetDetailsAsync(int saleId, CancellationToken cancellationToken = default)
    {
        return await _context.SaleDetails
            .AsNoTracking()
            .Include(d => d.Product)
            .Where(d => d.SaleId == saleId)
            .OrderBy(d => d.Product!.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> SearchAsync(
        int? productId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // Se incluyen los productos para mostrar sus nombres y unidades en el historial.
        // Más recientes primero; el Id desempata ventas con la misma fecha.
        return await Filter(productId, fromDate, toDate)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> SearchPageAsync(
        int? productId, DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Filter(productId, fromDate, toDate);
        var total = await query.CountAsync(cancellationToken);

        // Primero se eligen las ventas de la página y después se cargan sus detalles.
        var items = await query
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    // Filtros comunes del historial de ventas.
    private IQueryable<Sale> Filter(int? productId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Sales.AsNoTracking();

        if (productId.HasValue)
            query = query.Where(s => s.Details.Any(d => d.ProductId == productId.Value));

        if (fromDate.HasValue)
            query = query.Where(s => s.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.Date <= toDate.Value);

        return query;
    }

    public Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken cancellationToken = default)
        => SearchAsync(cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        => SearchAsync(fromDate: fromDate, toDate: toDate, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Sale>> GetByProductAsync(int productId, CancellationToken cancellationToken = default)
        => SearchAsync(productId: productId, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<ProductSalesByDayDto>> GetSoldQuantitiesByDayAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.SaleDetails
            .AsNoTracking()
            .Where(d => d.Sale!.Date >= fromDate && d.Sale.Date <= toDate)
            .GroupBy(d => new { d.ProductId, d.Product!.Name, d.Product.SKU, Day = d.Sale!.Date.Date })
            .Select(g => new ProductSalesByDayDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                SKU = g.Key.SKU,
                Day = g.Key.Day,
                Quantity = g.Sum(d => d.Quantity)
            })
            .OrderBy(r => r.Day)
            .ThenBy(r => r.ProductName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> GetDailyQuantitiesAsync(int productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        // Solo los detalles de este producto: si la venta tiene otros productos, no se cuentan.
        var from = fromDate.Date;
        var to = toDate.Date.AddDays(1);

        var rows = await _context.SaleDetails
            .AsNoTracking()
            .Where(d => d.ProductId == productId && d.Sale!.Date >= from && d.Sale.Date < to)
            .GroupBy(d => d.Sale!.Date.Date)
            .Select(g => new { Day = g.Key, Quantity = g.Sum(d => d.Quantity) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Day, r => r.Quantity);
    }

    public async Task<DateTime?> GetFirstSaleDateAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sales
            .AsNoTracking()
            .OrderBy(s => s.Date)
            .Select(s => (DateTime?)s.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DateTime?> GetFirstSaleDateAsync(int productId, CancellationToken cancellationToken = default)
    {
        // Usa el índice de SaleDetails.ProductId; solo devuelve una fecha.
        return await _context.SaleDetails
            .AsNoTracking()
            .Where(d => d.ProductId == productId)
            .Select(d => (DateTime?)d.Sale!.Date)
            .MinAsync(cancellationToken);
    }
}
