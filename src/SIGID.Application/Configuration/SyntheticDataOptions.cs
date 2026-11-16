namespace SIGID.Application.Configuration;

/// <summary>
/// Configuración del generador de datos históricos de prueba (sección "SyntheticData").
/// </summary>
public class SyntheticDataOptions
{
    /// <summary>
    /// Deshabilitado por defecto. Solo se activa en desarrollo o pruebas.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Máximo de productos por lote.
    /// </summary>
    public int MaxProductsPerBatch { get; set; } = 20;

    /// <summary>
    /// Máximo de días por lote (24 meses).
    /// </summary>
    public int MaxDays { get; set; } = 731;

    /// <summary>
    /// Máximo de registros (producto × día) por ejecución, para no sobrecargar SQL Server.
    /// </summary>
    public int MaxRecordsPerBatch { get; set; } = 15000;
}
