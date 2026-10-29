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

    Task<IReadOnlyList<Sale>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetByProductAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cantidades vendidas por producto y día dentro de un período (para el futuro análisis de demanda).
    /// </summary>
    Task<IReadOnlyList<ProductSalesByDayDto>> GetSoldQuantitiesByDayAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
