using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Interfaces.Forecasting;

/// <summary>
/// Motor de predicción de series temporales. La implementación con ML.NET (SSA)
/// está en Infrastructure, así Application no depende de ML.NET.
/// </summary>
public interface IDemandForecastEngine
{
    /// <summary>
    /// Entrena el modelo con la serie diaria indicada (ya ordenada y sin huecos)
    /// y devuelve las estimaciones de los siguientes <paramref name="horizon"/> días.
    /// </summary>
    ForecastEngineResult Forecast(IReadOnlyList<float> trainingSeries, int horizon);
}
