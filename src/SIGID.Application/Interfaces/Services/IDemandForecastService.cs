using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Interfaces.Services;

/// <summary>
/// Predicción de demanda por producto con ML.NET/SSA (HU-19, HU-20).
/// </summary>
public interface IDemandForecastService
{
    Task<DemandForecastResultDto> ForecastAsync(DemandForecastRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si un producto tiene historial suficiente para pronosticar y para evaluar.
    /// </summary>
    Task<DemandDataAvailabilityDto> GetAvailabilityAsync(int productId, SalesDataSource dataSource, CancellationToken cancellationToken = default);
}
