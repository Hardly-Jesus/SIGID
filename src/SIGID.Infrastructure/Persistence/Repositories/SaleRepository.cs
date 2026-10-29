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
        var query = _context.Sales
            .AsNoTracking()
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(s => s.Details.Any(d => d.ProductId == productId.Value));

        if (fromDate.HasValue)
            query = query.Where(s => s.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.Date <= toDate.Value);

        // Más recientes primero; el Id desempata ventas con la misma fecha.
        return await query
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.Id)
            .ToListAsync(cancellationToken);
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
}
