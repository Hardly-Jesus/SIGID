using SIGID.Domain.Entities;
using SIGID.Domain.Enums;

namespace SIGID.Application.Interfaces.Repositories;

public interface IInventoryMovementRepository
{
    /// <summary>
    /// Registra el movimiento y actualiza las existencias del producto en una sola transacción.
    /// En las salidas solo descuenta si hay existencias suficientes en ese momento.
    /// Devuelve false si la salida no pudo aplicarse por falta de existencias.
    /// </summary>
    Task<bool> RegisterAsync(InventoryMovement movement, CancellationToken cancellationToken = default);

    /// <summary>
    /// Existencias actuales del producto, o null si el producto no existe.
    /// </summary>
    Task<int?> GetCurrentStockAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta movimientos combinando filtros opcionales.
    /// Siempre ordena del más reciente al más antiguo.
    /// </summary>
    Task<IReadOnlyList<InventoryMovement>> SearchAsync(
        int? productId = null,
        MovementType? movementType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> GetByProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> GetByTypeAsync(MovementType movementType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}
