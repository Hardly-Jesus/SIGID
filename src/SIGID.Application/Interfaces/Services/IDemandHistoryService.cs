using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Interfaces.Services;

/// <summary>
/// Prepara la serie diaria de ventas de un producto (real o de prueba) para el análisis y el modelo.
/// </summary>
public interface IDemandHistoryService
{
    /// <summary>
    /// Serie diaria del producto entre las fechas indicadas, ordenada y con 0 en los días
    /// conocidos sin ventas. El producto debe existir.
    /// </summary>
    Task<DemandSeriesDto> GetDailySeriesAsync(int productId, SalesDataSource dataSource, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
