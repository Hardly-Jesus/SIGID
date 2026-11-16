using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Interfaces.Services;

/// <summary>
/// Generación y consulta de datos históricos de prueba (HU-17).
/// </summary>
public interface ISyntheticSalesService
{
    /// <summary>
    /// Indica si la generación está habilitada en la configuración.
    /// </summary>
    bool IsGenerationEnabled { get; }

    Task<HistoricalSalesGenerationResultDto> GenerateAsync(HistoricalSalesGenerationDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SyntheticSalesBatchDto>> GetBatchesAsync(CancellationToken cancellationToken = default);

    Task<bool> DeleteBatchAsync(int batchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registros generados de un producto (o de todos) en un período.
    /// </summary>
    Task<IReadOnlyList<HistoricalSalesRecordDto>> GetRecordsAsync(int? productId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
