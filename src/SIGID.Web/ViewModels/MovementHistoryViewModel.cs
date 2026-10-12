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

    public int TotalEntries => Movements.Where(m => m.MovementType == MovementType.Entry).Sum(m => m.Quantity);

    public int TotalExits => Movements.Where(m => m.MovementType == MovementType.Exit).Sum(m => m.Quantity);

    public MovementFilterDto ToFilter() => new()
    {
        ProductId = ProductId,
        MovementType = Type,
        FromDate = From,
        ToDate = To
    };
}
