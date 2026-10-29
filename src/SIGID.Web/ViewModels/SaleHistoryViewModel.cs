using SIGID.Application.DTOs;
using SIGID.Application.DTOs.Sales;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Filtros y resultados del historial de ventas (HU-16).
/// </summary>
public class SaleHistoryViewModel
{
    public int? ProductId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    public IReadOnlyList<SaleHistoryDto> Sales { get; set; } = Array.Empty<SaleHistoryDto>();

    public bool HasFilters => ProductId.HasValue || From.HasValue || To.HasValue;

    public SaleHistoryFilterDto ToFilter() => new()
    {
        ProductId = ProductId,
        StartDate = From,
        EndDate = To
    };
}
