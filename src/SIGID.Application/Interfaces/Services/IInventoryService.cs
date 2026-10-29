using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Sales;

namespace SIGID.Application.Interfaces.Services;

public interface IInventoryService
{
    /// <summary>
    /// Registra una entrada: suma la cantidad a las existencias y guarda el movimiento.
    /// </summary>
    Task<InventoryMovementDto> RegisterEntryAsync(RegisterMovementDto entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra una salida: valida existencias suficientes, resta la cantidad y guarda el movimiento.
    /// </summary>
    Task<InventoryMovementDto> RegisterExitAsync(RegisterMovementDto exit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra las salidas generadas por una venta (un movimiento por producto) usando
    /// las mismas reglas que las salidas manuales. Debe llamarse dentro de la transacción de la venta.
    /// </summary>
    Task<IReadOnlyList<InventoryMovementDto>> RegisterSaleExitsAsync(int saleId, DateTime date, IReadOnlyList<RegisterSaleDetailDto> items, CancellationToken cancellationToken = default);

    Task<int?> GetCurrentStockAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovementDto>> GetEntriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovementDto>> GetExitsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial con filtros combinables, ordenado del más reciente al más antiguo.
    /// </summary>
    Task<IReadOnlyList<InventoryMovementDto>> GetHistoryAsync(MovementFilterDto filter, CancellationToken cancellationToken = default);
}
