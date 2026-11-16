using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;
using SIGID.Application.Interfaces.Forecasting;

namespace SIGID.Infrastructure.Forecasting;

/// <summary>
/// Predicción de demanda con ML.NET y el algoritmo Singular Spectrum Analysis (SSA).
/// Recibe una serie diaria ya preparada (ordenada, sin huecos) y devuelve el pronóstico.
/// </summary>
/// <remarks>
/// SSA de ML.NET pronostica cada día como una combinación de los últimos días observados y
/// no resta el nivel medio de la serie. Si los últimos días fueron todos 0 (algo común en
/// productos de venta intermitente), el pronóstico completo salía en 0 aunque el producto se
/// venda con regularidad. Por eso la serie se centra: se resta el promedio del entrenamiento,
/// SSA modela las variaciones (tendencia y ciclo semanal) y luego se vuelve a sumar el promedio.
/// </remarks>
public class SsaDemandForecastEngine : IDemandForecastEngine
{
    // Semilla fija: con los mismos datos, el modelo da siempre el mismo resultado.
    private const int MlSeed = 1;

    private readonly DemandForecastOptions _options;

    public SsaDemandForecastEngine(DemandForecastOptions options)
    {
        _options = options;
    }

    public ForecastEngineResult Forecast(IReadOnlyList<float> trainingSeries, int horizon)
    {
        if (horizon <= 0)
            throw new ArgumentOutOfRangeException(nameof(horizon), "El horizonte debe ser mayor que cero.");

        // La ventana depende de cuántos días hay para entrenar.
        var windowSize = _options.WindowSizeFor(trainingSeries.Count);
        var seriesLength = _options.SeriesLengthFor(windowSize);

        // ML.NET exige más observaciones que el doble de la ventana de análisis.
        if (trainingSeries.Count <= 2 * windowSize)
            throw new ArgumentException("La serie es demasiado corta para la ventana de análisis configurada.");

        if (trainingSeries.Any(v => !float.IsFinite(v) || v < 0))
            throw new ArgumentException("La serie contiene cantidades inválidas.");

        // Centramos la serie en su promedio (ver la explicación de la clase).
        var mean = trainingSeries.Average();

        var mlContext = new MLContext(seed: MlSeed);
        var data = mlContext.Data.LoadFromEnumerable(trainingSeries.Select(q => new SalesObservation { Quantity = q - mean }));

        var pipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(SalesForecast.Forecast),
            inputColumnName: nameof(SalesObservation.Quantity),
            windowSize: windowSize,
            seriesLength: seriesLength,
            trainSize: trainingSeries.Count,          // se entrena con toda la serie recibida
            horizon: horizon,
            confidenceLevel: _options.ConfidenceLevel,
            confidenceLowerBoundColumn: nameof(SalesForecast.LowerBound),
            confidenceUpperBoundColumn: nameof(SalesForecast.UpperBound));

        var model = pipeline.Fit(data);

        // El motor pronostica los días que siguen al último dato de entrenamiento.
        var engine = model.CreateTimeSeriesEngine<SalesObservation, SalesForecast>(mlContext);
        var prediction = engine.Predict();

        // Se vuelve a sumar el promedio para obtener cantidades.
        return new ForecastEngineResult
        {
            Forecast = prediction.Forecast.Select(v => v + mean).ToArray(),
            LowerBound = prediction.LowerBound.Select(v => v + mean).ToArray(),
            UpperBound = prediction.UpperBound.Select(v => v + mean).ToArray(),
            WindowSize = windowSize,
            SeriesLength = seriesLength,
            TrainingMean = mean
        };
    }
}

/// <summary>
/// Cantidad vendida en un día (entrada de ML.NET). Solo se usa dentro de Infrastructure.
/// </summary>
internal class SalesObservation
{
    public float Quantity { get; set; }
}

/// <summary>
/// Salida de ML.NET: valores pronosticados y sus límites.
/// </summary>
internal class SalesForecast
{
    public float[] Forecast { get; set; } = Array.Empty<float>();

    public float[] LowerBound { get; set; } = Array.Empty<float>();

    public float[] UpperBound { get; set; } = Array.Empty<float>();
}
