using SIGID.Application.Interfaces.Persistence;

namespace SIGID.Infrastructure.Persistence;

/// <summary>
/// Ejecuta una operación dentro de una transacción de SQL Server.
/// Si la operación falla, se revierte todo lo que hizo.
/// </summary>
public class EfTransactionRunner : ITransactionRunner
{
    private readonly SigidDbContext _context;

    public EfTransactionRunner(SigidDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        // Si ya existe una transacción, la operación forma parte de ella.
        if (_context.Database.CurrentTransaction is not null)
            return await operation();

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            // Se descartan las entidades pendientes para que el contexto quede limpio.
            _context.ChangeTracker.Clear();
            throw;
        }
    }
}
