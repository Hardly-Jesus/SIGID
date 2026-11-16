namespace SIGID.Web.ViewModels;

/// <summary>
/// Tipo de línea del gráfico. Cada tipo tiene su color y estilo en site.css.
/// </summary>
public enum DemandChartSeriesKind
{
    History,
    Forecast,
    Actual,
    Reference
}

/// <summary>
/// Una línea del gráfico. Values tiene un valor por cada etiqueta (null = sin dato).
/// </summary>
public class DemandChartSeries
{
    public string Name { get; set; } = string.Empty;

    public DemandChartSeriesKind Kind { get; set; }

    public double?[] Values { get; set; } = Array.Empty<double?>();
}

/// <summary>
/// Datos para el gráfico de líneas de demanda (_DemandChart). Se dibuja en el servidor con SVG,
/// sin bibliotecas externas. Solo muestra los valores recibidos: no calcula nada nuevo.
/// </summary>
public class DemandChartViewModel
{
    /// <summary>
    /// Descripción para lectores de pantalla.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Etiquetas del eje temporal, en orden cronológico.
    /// </summary>
    public List<string> Labels { get; set; } = new();

    public List<DemandChartSeries> Series { get; set; } = new();

    /// <summary>
    /// Límites de predicción (opcionales), alineados con Labels.
    /// </summary>
    public double?[]? LowerBound { get; set; }

    public double?[]? UpperBound { get; set; }

    public string BandName { get; set; } = "Rango estimado";

    /// <summary>
    /// Posición de la primera etiqueta pronosticada: ahí se dibuja la división entre historial y pronóstico.
    /// </summary>
    public int? DividerIndex { get; set; }

    public string? DividerLabel { get; set; }

    public bool HasBand => LowerBound is not null && UpperBound is not null;
}
