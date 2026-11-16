namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Comportamiento de las ventas de un producto en el historial de entrenamiento.
/// </summary>
public enum DemandBehavior
{
    /// <summary>No hay datos suficientes para clasificar.</summary>
    InsufficientHistory = 0,

    /// <summary>Se vende en la mayoría de los días.</summary>
    Frequent = 1,

    /// <summary>Se vende algunos días por semana.</summary>
    Moderate = 2,

    /// <summary>Se vende pocas veces: muchos días sin ventas.</summary>
    Sporadic = 3
}

/// <summary>
/// Indicadores simples del historial usado para entrenar (sin modelos estadísticos adicionales).
/// </summary>
public class DemandProfileDto
{
    public DemandBehavior Behavior { get; set; }

    /// <summary>
    /// Días con información en el historial (incluye los días sin ventas).
    /// </summary>
    public int ObservedDays { get; set; }

    public int DaysWithSales { get; set; }

    /// <summary>
    /// Porcentaje de días sin ventas (0–100).
    /// </summary>
    public double ZeroDayPercent { get; set; }

    public double TotalQuantity { get; set; }

    /// <summary>
    /// Promedio diario, contando también los días sin ventas.
    /// </summary>
    public double AverageDailyQuantity { get; set; }

    /// <summary>
    /// Promedio de los días en que sí hubo ventas.
    /// </summary>
    public double AverageQuantityWhenSold { get; set; }

    /// <summary>
    /// Días seguidos sin ventas al final del historial.
    /// </summary>
    public int TrailingDaysWithoutSales { get; set; }

    /// <summary>
    /// true si el producto tuvo ventas antes, pero no en los últimos días configurados.
    /// </summary>
    public bool StoppedSelling { get; set; }
}
