using SIGID.Domain.Enums;

namespace SIGID.Domain.Entities;

/// <summary>
/// Lote de ventas históricas de prueba (datos sintéticos, HU-17).
/// Se guarda aparte de las ventas reales: no es una venta ni mueve el inventario.
/// </summary>
public class SyntheticSalesBatch
{
    public int Id { get; set; }

    /// <summary>
    /// Fecha y hora en que se generó el lote.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Parámetros usados para generar el lote (permiten reproducirlo).
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DemandPattern Pattern { get; set; }

    public int RandomSeed { get; set; }

    /// <summary>
    /// Identificadores de los productos incluidos, separados por comas (por ejemplo "1,4,7").
    /// </summary>
    public string ProductIds { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public int RecordCount { get; set; }

    /// <summary>
    /// Resumen único de los parámetros. Evita cargar dos veces el mismo lote por error.
    /// </summary>
    public string ParametersKey { get; set; } = string.Empty;

    public ICollection<SyntheticSalesRecord> Records { get; set; } = new List<SyntheticSalesRecord>();
}
