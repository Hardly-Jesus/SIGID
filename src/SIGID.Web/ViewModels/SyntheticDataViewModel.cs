using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Demand;
using SIGID.Domain.Enums;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Herramienta de desarrollo para generar datos históricos de prueba (HU-17).
/// </summary>
public class SyntheticDataViewModel
{
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public List<int> ProductIds { get; set; } = new();

    public DemandPattern DemandPattern { get; set; } = DemandPattern.Stable;

    public int RandomSeed { get; set; } = 1;

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    public IReadOnlyList<SyntheticSalesBatchDto> Batches { get; set; } = Array.Empty<SyntheticSalesBatchDto>();

    public int MaxProducts { get; set; }

    public HistoricalSalesGenerationDto ToDto() => new()
    {
        StartDate = StartDate,
        EndDate = EndDate,
        ProductIds = ProductIds,
        DemandPattern = DemandPattern,
        RandomSeed = RandomSeed
    };
}
