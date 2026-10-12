using SIGID.Application.DTOs;
using SIGID.Domain.Enums;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Listado de entradas o de salidas de inventario.
/// </summary>
public class MovementListViewModel
{
    public MovementType MovementType { get; set; }

    public IReadOnlyList<InventoryMovementDto> Movements { get; set; } = Array.Empty<InventoryMovementDto>();

    public bool IsEntry => MovementType == MovementType.Entry;
}
