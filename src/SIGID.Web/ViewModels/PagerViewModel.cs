namespace SIGID.Web.ViewModels;

/// <summary>
/// Datos de la paginación de un historial (se conserva el resto de los filtros en los enlaces).
/// </summary>
public class PagerViewModel
{
    /// <summary>
    /// Registros por página de los historiales.
    /// </summary>
    public const int DefaultPageSize = 50;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public int TotalCount { get; set; }

    public int TotalPages { get; set; } = 1;

    /// <summary>
    /// Nombre de los registros en plural, por ejemplo "ventas".
    /// </summary>
    public string ItemsName { get; set; } = "registros";

    public string Action { get; set; } = "Index";

    /// <summary>
    /// Filtros actuales, para no perderlos al cambiar de página.
    /// </summary>
    public Dictionary<string, string?> RouteValues { get; set; } = new();

    public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;

    public int LastItem => Math.Min(Page * PageSize, TotalCount);

    public Dictionary<string, string?> RouteFor(int page)
    {
        var values = new Dictionary<string, string?>(RouteValues) { ["page"] = page.ToString() };
        return values;
    }
}
