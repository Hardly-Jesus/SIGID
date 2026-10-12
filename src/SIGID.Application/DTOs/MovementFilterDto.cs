using SIGID.Domain.Enums;

namespace SIGID.Application.DTOs;

/// <summary>
/// Filtros del historial de movimientos. Los valores nulos no filtran.
/// </summary>
public class MovementFilterDto
{
    public int? ProductId { get; set; }

    public MovementType? MovementType { get; set; }

    /// <summary>
    /// Fecha inicial (se incluye el día completo).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Fecha final (se incluye el día completo).
    /// </summary>
    public DateTime? ToDate { get; set; }
}
