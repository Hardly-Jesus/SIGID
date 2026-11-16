namespace SIGID.Domain.Enums;

/// <summary>
/// Comportamiento de demanda usado para generar datos históricos de prueba (HU-17).
/// </summary>
public enum DemandPattern
{
    Stable = 1,
    Increasing = 2,
    Decreasing = 3,
    Seasonal = 4
}
