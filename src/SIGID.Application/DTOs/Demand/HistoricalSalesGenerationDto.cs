using SIGID.Domain.Enums;

namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Parámetros para generar datos históricos de prueba (HU-17).
/// </summary>
public class HistoricalSalesGenerationDto
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<int> ProductIds { get; set; } = new();

    public DemandPattern DemandPattern { get; set; } = DemandPattern.Stable;

    /// <summary>
    /// Con la misma semilla y los mismos parámetros se obtienen exactamente los mismos datos.
    /// </summary>
    public int RandomSeed { get; set; }
}

/// <summary>
/// Un registro histórico: producto, día y cantidad vendida.
/// </summary>
public class HistoricalSalesRecordDto
{
    public int ProductId { get; set; }

    public DateTime Date { get; set; }

    public int Quantity { get; set; }
}

/// <summary>
/// Resultado de una generación de datos de prueba.
/// </summary>
public class HistoricalSalesGenerationResultDto
{
    public int BatchId { get; set; }

    public int GeneratedRecords { get; set; }

    public int ProductCount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsSuccessful { get; set; }

    public string? ErrorMessage { get; set; }

    public static HistoricalSalesGenerationResultDto Failure(string message) => new()
    {
        IsSuccessful = false,
        ErrorMessage = message
    };
}

/// <summary>
/// Resumen de un lote generado, para listarlo.
/// </summary>
public class SyntheticSalesBatchDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DemandPattern Pattern { get; set; }

    public int RandomSeed { get; set; }

    public int ProductCount { get; set; }

    public int RecordCount { get; set; }

    public List<string> ProductNames { get; set; } = new();
}
