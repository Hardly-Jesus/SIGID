using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Interfaces.Services;

/// <summary>
/// Análisis histórico de ventas por producto (HU-18).
/// </summary>
public interface ISalesAnalysisService
{
    Task<SalesAnalysisResultDto> AnalyzeAsync(SalesAnalysisFilterDto filter, CancellationToken cancellationToken = default);
}
