using SIGID.Domain.Entities;

namespace SIGID.Application.Interfaces.Repositories;

/// <summary>
/// Acceso a los datos históricos de prueba (HU-17). Nunca toca ventas reales ni inventario.
/// </summary>
public interface ISyntheticSalesRepository
{
    /// <summary>
    /// Guarda el lote con todos sus registros en una sola operación: o se guarda todo o nada.
    /// </summary>
    Task AddBatchAsync(SyntheticSalesBatch batch, CancellationToken cancellationToken = default);

    Task<SyntheticSalesBatch?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyntheticSalesBatch>> GetBatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si ya existe un lote generado con exactamente los mismos parámetros.
    /// </summary>
    Task<SyntheticSalesBatch?> GetBatchByKeyAsync(string parametersKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Productos (de la lista) que ya tienen datos de prueba dentro del período indicado.
    /// </summary>
    Task<IReadOnlyList<int>> GetProductsWithDataAsync(IReadOnlyCollection<int> productIds, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un lote y sus registros. Devuelve false si no existe.
    /// </summary>
    Task<bool> DeleteBatchAsync(int batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registros de un producto (o de todos) dentro de un período, ordenados por fecha.
    /// </summary>
    Task<IReadOnlyList<SyntheticSalesRecord>> GetRecordsAsync(int? productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cantidad total por día de un producto (los días sin registros no aparecen).
    /// </summary>
    Task<IReadOnlyDictionary<DateTime, int>> GetDailyQuantitiesAsync(int productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Períodos (inicio, fin) de los lotes que incluyen al producto: los días que tienen información.
    /// </summary>
    Task<IReadOnlyList<(DateTime StartDate, DateTime EndDate)>> GetCoverageAsync(int productId, CancellationToken cancellationToken = default);
}
