using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Métricas de precisión (HU-21). Fórmulas, con e = real − estimado y n días evaluados:
/// - MAE  = Σ|e| / n
/// - RMSE = √(Σe² / n)
/// - WAPE = Σ|e| / Σ|real| × 100 (no se puede calcular si en el período no hubo ventas).
/// </summary>
public static class ForecastMetrics
{
    /// <summary>
    /// Diferencia de MAE por debajo de la cual se consideran equivalentes (se muestran 2 decimales).
    /// </summary>
    public const double EqualTolerance = 0.005;

    public static ForecastMetricsDto Calculate(IReadOnlyList<double> actual, IReadOnlyList<double> predicted)
    {
        if (actual.Count == 0)
            throw new ArgumentException("No hay valores para evaluar.");

        if (actual.Count != predicted.Count)
            throw new ArgumentException("Las cantidades reales y estimadas deben tener la misma longitud.");

        if (actual.Concat(predicted).Any(v => double.IsNaN(v) || double.IsInfinity(v)))
            throw new ArgumentException("Las cantidades contienen valores numéricos inválidos.");

        double absoluteSum = 0, squaredSum = 0, actualSum = 0;
        for (var i = 0; i < actual.Count; i++)
        {
            var error = actual[i] - predicted[i];
            absoluteSum += Math.Abs(error);
            squaredSum += error * error;
            actualSum += Math.Abs(actual[i]);
        }

        return new ForecastMetricsDto
        {
            MAE = absoluteSum / actual.Count,
            RMSE = Math.Sqrt(squaredSum / actual.Count),
            // Evitamos dividir entre cero: sin ventas reales, WAPE no está disponible.
            WAPE = actualSum > 0 ? absoluteSum / actualSum * 100 : null
        };
    }

    /// <summary>
    /// Compara los dos modelos por su MAE en el mismo período evaluado.
    /// </summary>
    public static ModelComparison Compare(double ssaMae, double baselineMae)
    {
        if (Math.Abs(ssaMae - baselineMae) < EqualTolerance)
            return ModelComparison.Equal;

        return ssaMae < baselineMae ? ModelComparison.SsaBetter : ModelComparison.BaselineBetter;
    }

    /// <summary>
    /// Modelo de referencia ingenuo: repite el último valor observado del entrenamiento.
    /// </summary>
    public static List<double> NaiveForecast(IReadOnlyList<double> training, int horizon)
    {
        if (training.Count == 0)
            throw new ArgumentException("No hay datos de entrenamiento para el modelo de referencia.");

        var lastValue = training[^1];
        return Enumerable.Repeat(lastValue, horizon).ToList();
    }
}
