namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Origen de los datos históricos. Nunca se mezclan: cada consulta usa uno solo.
/// </summary>
public enum SalesDataSource
{
    /// <summary>Ventas reales registradas en Sales y SaleDetails.</summary>
    Real = 1,

    /// <summary>Datos de prueba generados con la HU-17.</summary>
    Synthetic = 2
}
