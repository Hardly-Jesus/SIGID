namespace SIGID.Application.DTOs;

/// <summary>
/// Una página de resultados de un listado grande (historiales).
/// </summary>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int Page { get; set; } = 1;

    public int PageSize { get; set; }

    /// <summary>
    /// Total de registros que cumplen los filtros (en todas las páginas).
    /// </summary>
    public int TotalCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

/// <summary>
/// Página del historial de movimientos, con los totales de todo el resultado filtrado
/// (no solo de la página): se calculan en la base de datos.
/// </summary>
public class MovementHistoryPageDto : PagedResultDto<InventoryMovementDto>
{
    public int TotalEntries { get; set; }

    public int TotalExits { get; set; }
}
