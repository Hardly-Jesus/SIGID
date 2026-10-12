using Microsoft.EntityFrameworkCore;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly SigidDbContext _context;

    public SupplierRepository(SigidDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Supplier?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();

        return await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<Supplier?> GetByIdentificationAsync(string identification, CancellationToken cancellationToken = default)
    {
        var normalized = identification.Trim();

        return await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Identification == normalized, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AnyAsync(s => s.Id == id, cancellationToken);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _context.Suppliers.Add(supplier);
        await PersistenceErrors.SaveChangesAsync(_context, "No se pudo guardar el proveedor. Verifique que la identificación/RNC no esté repetida.", cancellationToken);
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(supplier).State == EntityState.Detached)
            _context.Suppliers.Update(supplier);

        await PersistenceErrors.SaveChangesAsync(_context, "No se pudo actualizar el proveedor. Verifique que la identificación/RNC no esté repetida.", cancellationToken);
    }

    public async Task DeleteAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _context.Suppliers.Remove(supplier);
        await PersistenceErrors.SaveChangesAsync(_context, "No se pudo eliminar el proveedor porque está siendo utilizado.", cancellationToken);
    }

    public async Task<bool> HasProductsAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(p => p.SupplierId == supplierId, cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetProductCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .GroupBy(p => p.SupplierId)
            .Select(g => new { SupplierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Count, cancellationToken);
    }
}
