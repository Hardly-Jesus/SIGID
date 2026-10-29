using Microsoft.EntityFrameworkCore;
using SIGID.Application.Interfaces.Repositories;
using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Infrastructure.Persistence.Repositories;

public class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly SigidDbContext _context;

    public InventoryMovementRepository(SigidDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RegisterAsync(InventoryMovement movement, CancellationToken cancellationToken = default)
    {
        // El cambio de existencias y el movimiento se guardan juntos: o se guardan ambos o ninguno.
        // Si ya hay una transacción abierta (por ejemplo, la de una venta), se usa esa misma
        // y quien la abrió decide si confirmar o revertir.
        var ownsTransaction = _context.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await _context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var quantity = movement.Quantity;
        int rowsUpdated;

        if (movement.MovementType == MovementType.Entry)
        {
            rowsUpdated = await _context.Products
                .Where(p => p.Id == movement.ProductId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CurrentStock, p => p.CurrentStock + quantity), cancellationToken);
        }
        else
        {
            // La condición CurrentStock >= quantity se evalúa en la misma sentencia UPDATE.
            // SQL Server bloquea la fila, así que dos salidas simultáneas no pueden
            // consumir las mismas existencias.
            rowsUpdated = await _context.Products
                .Where(p => p.Id == movement.ProductId && p.CurrentStock >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CurrentStock, p => p.CurrentStock - quantity), cancellationToken);
        }

        if (rowsUpdated == 0)
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        movement.StockAfter = await _context.Products
            .Where(p => p.Id == movement.ProductId)
            .Select(p => p.CurrentStock)
            .SingleAsync(cancellationToken);

        _context.InventoryMovements.Add(movement);
        await PersistenceErrors.SaveChangesAsync(_context, "No se pudo registrar el movimiento de inventario.", cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<int?> GetCurrentStockAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.Id == productId)
            .Select(p => (int?)p.CurrentStock)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryMovement>> SearchAsync(
        int? productId = null,
        MovementType? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .AsQueryable();

        if (productId.HasValue)
            query = query.Where(m => m.ProductId == productId.Value);

        if (movementType.HasValue)
            query = query.Where(m => m.MovementType == movementType.Value);

        if (fromDate.HasValue)
            query = query.Where(m => m.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(m => m.Date <= toDate.Value);

        // Más recientes primero; el Id desempata movimientos con la misma fecha.
        return await query
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<InventoryMovement>> GetAllAsync(CancellationToken cancellationToken = default)
        => SearchAsync(cancellationToken: cancellationToken);

    public Task<IReadOnlyList<InventoryMovement>> GetByProductAsync(int productId, CancellationToken cancellationToken = default)
        => SearchAsync(productId: productId, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<InventoryMovement>> GetByTypeAsync(MovementType movementType, CancellationToken cancellationToken = default)
        => SearchAsync(movementType: movementType, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<InventoryMovement>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        => SearchAsync(fromDate: fromDate, toDate: toDate, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<InventoryMovement>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryMovements
            .AsNoTracking()
            .Include(m => m.Product)
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
