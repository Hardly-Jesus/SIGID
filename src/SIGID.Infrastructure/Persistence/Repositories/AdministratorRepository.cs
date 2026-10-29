using Microsoft.EntityFrameworkCore;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;

namespace SIGID.Infrastructure.Persistence.Repositories;

public class AdministratorRepository : IAdministratorRepository
{
    private readonly SigidDbContext _context;

    public AdministratorRepository(SigidDbContext context)
    {
        _context = context;
    }

    public async Task<Administrator?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Administrators
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Administrator?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var normalized = userName.Trim().ToLower();

        return await _context.Administrators
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserName.ToLower() == normalized, cancellationToken);
    }

    public async Task<Administrator?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();

        return await _context.Administrators
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<Administrator?> GetByUserNameOrEmailAsync(string userNameOrEmail, CancellationToken cancellationToken = default)
    {
        var normalized = userNameOrEmail.Trim().ToLower();

        return await _context.Administrators
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserName.ToLower() == normalized || a.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Administrators.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Administrators
            .AnyAsync(a => a.Id == id && a.IsActive, cancellationToken);
    }

    public async Task AddAsync(Administrator administrator, CancellationToken cancellationToken = default)
    {
        _context.Administrators.Add(administrator);
        await PersistenceErrors.SaveChangesAsync(_context, "No se pudo crear el administrador. Verifique que el usuario y el correo no estén repetidos.", cancellationToken);
    }
}
