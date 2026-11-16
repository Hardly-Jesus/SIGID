using SIGID.Application.DTOs;
using SIGID.Domain.Enums;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Filtros y resultados del historial de movimientos.
/// </summary>
public class MovementHistoryViewModel
{
    public int? ProductId { get; set; }

    public MovementType? Type { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    public IReadOnlyList<InventoryMovementDto> Movements { get; set; } = Array.Empty<InventoryMovementDto>();

    public bool HasFilters => ProductId.HasValue || Type.HasValue || From.HasValue || To.HasValue;

    /// <summary>
    /// Totales de todo el resultado filtrado (no solo de la página visible).
    /// </summary>
    public int TotalEntries { get; set; }

    public int TotalExits { get; set; }

    /// <summary>
    /// Total de movimientos con los filtros aplicados (todas las páginas).
    /// </summary>
    public int TotalCount { get; set; }

    public PagerViewModel Pager { get; set; } = new();

    public MovementFilterDto ToFilter() => new()
    {
        ProductId = ProductId,
        MovementType = Type,
        FromDate = From,
        ToDate = To
    };
}
