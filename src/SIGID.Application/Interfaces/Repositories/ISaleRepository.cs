using SIGID.Application.DTOs.Sales;
using SIGID.Domain.Entities;

namespace SIGID.Application.Interfaces.Repositories;

public interface ISaleRepository
{
    /// <summary>
    /// Guarda la venta junto con sus detalles. Después de guardar, sale.Id queda asignado.
    /// </summary>
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Venta con sus detalles y los productos de cada detalle.
    /// </summary>
    Task<Sale?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Sale?> GetByRequestIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleDetail>> GetDetailsAsync(int saleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta ventas combinando filtros opcionales. Siempre ordena de la más reciente a la más antigua.
    /// </summary>
    Task<IReadOnlyList<Sale>> SearchAsync(
        int? productId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Una página del historial de ventas filtrado (más recientes primero) y el total de ventas.
    /// </summary>
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> SearchPageAsync(
        int? productId, DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetByProductAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cantidades vendidas por producto y día dentro de un período (para el futuro análisis de demanda).
    /// </summary>
    Task<IReadOnlyList<ProductSalesByDayDto>> GetSoldQuantitiesByDayAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cantidad vendida por día de un solo producto (EP-05). Solo cuenta los detalles de ese producto.
    /// Los días sin ventas no aparecen.
    /// </summary>
    Task<IReadOnlyDictionary<DateTime, int>> GetDailyQuantitiesAsync(int productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fecha de la primera venta registrada en el sistema (null si todavía no hay ventas).
    /// Desde ese día SIGID tiene información de ventas.
    /// </summary>
    Task<DateTime?> GetFirstSaleDateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fecha de la primera venta registrada de un producto (null si nunca se vendió).
    /// </summary>
    Task<DateTime?> GetFirstSaleDateAsync(int productId, CancellationToken cancellationToken = default);
}
