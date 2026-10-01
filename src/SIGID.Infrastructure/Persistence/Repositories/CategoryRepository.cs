using Microsoft.EntityFrameworkCore;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio inicial de Categories. Las operaciones de registro y edición
/// se agregarán cuando se desarrolle su módulo.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly SigidDbContext _context;

    public CategoryRepository(SigidDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(x => x.Id == id, cancellationToken);
    }
}
