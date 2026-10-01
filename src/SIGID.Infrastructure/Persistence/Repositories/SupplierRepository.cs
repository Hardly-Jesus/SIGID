using Microsoft.EntityFrameworkCore;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio inicial de Suppliers. Las operaciones de registro y edición
/// se agregarán cuando se desarrolle su módulo.
/// </summary>
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
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AnyAsync(x => x.Id == id, cancellationToken);
    }
}
