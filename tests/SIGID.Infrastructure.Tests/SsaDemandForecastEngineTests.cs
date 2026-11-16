using SIGID.Application.Configuration;
using SIGID.Infrastructure.Forecasting;

namespace SIGID.Infrastructure.Tests;

/// <summary>
/// Pruebas del modelo real de ML.NET (SSA). No usan simulaciones.
/// </summary>
public class SsaDemandForecastEngineTests
{
    private readonly SsaDemandForecastEngine _engine = new(new DemandForecastOptions());

    /// <summary>
    /// Serie semanal: más ventas el fin de semana.
    /// </summary>
    private static List<float> WeeklySeries(int days)
        => Enumerable.Range(0, days).Select(i => i % 7 is 5 or 6 ? 9f : 4f).ToList();

    [Fact]
    public void Forecast_ReturnsRequestedHorizon_WithFiniteValuesAndBounds()
    {
        var result = _engine.Forecast(WeeklySeries(120), 14);

        Assert.Equal(14, result.Forecast.Length);
        Assert.Equal(14, result.LowerBound.Length);
        Assert.Equal(14, result.UpperBound.Length);
        Assert.All(result.Forecast, v => Assert.True(float.IsFinite(v)));
        for (var i = 0; i < 14; i++)
            Assert.True(result.LowerBound[i] <= result.Forecast[i] && result.Forecast[i] <= result.UpperBound[i]);
    }

    [Fact]
    public void Forecast_LearnsWeeklyPattern()
    {
        // La serie termina en un domingo (i = 118 → 118 % 7 = 6); el día siguiente es lunes (bajo).
        var result = _engine.Forecast(WeeklySeries(119), 7);

        var weekdays = new[] { result.Forecast[0], result.Forecast[1], result.Forecast[2], result.Forecast[3], result.Forecast[4] }.Average();
        var weekend = new[] { result.Forecast[5], result.Forecast[6] }.Average();
        Assert.True(weekend > weekdays + 2, $"fin de semana {weekend:F2} vs. semana {weekdays:F2}");
    }

    [Fact]
    public void Forecast_SameData_SameResult()
    {
        var series = Enumerable.Range(0, 90).Select(i => (float)(5 + Math.Sin(i / 3.0) * 2 + i % 4)).ToList();

        var first = _engine.Forecast(series, 10);
        var second = _engine.Forecast(series, 10);

        Assert.Equal(first.Forecast, second.Forecast);
    }

    [Fact]
    public void Forecast_ConstantAndZeroSeries_DoNotFail()
    {
        var constant = _engine.Forecast(Enumerable.Repeat(3f, 60).ToList(), 5);
        var zeros = _engine.Forecast(Enumerable.Repeat(0f, 60).ToList(), 5);

        Assert.All(constant.Forecast, v => Assert.Equal(3f, v, 2));
        Assert.All(zeros.Forecast, v => Assert.True(float.IsFinite(v)));
    }

    [Fact]
    public void Forecast_TooShortOrInvalidSeries_Throws()
    {
        Assert.Throws<ArgumentException>(() => _engine.Forecast(Enumerable.Repeat(1f, 14).ToList(), 5));
        Assert.Throws<ArgumentException>(() => _engine.Forecast(Enumerable.Repeat(1f, 59).Append(float.NaN).ToList(), 5));
        Assert.Throws<ArgumentException>(() => _engine.Forecast(Enumerable.Repeat(1f, 59).Append(-1f).ToList(), 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => _engine.Forecast(Enumerable.Repeat(1f, 60).ToList(), 0));
    }

    [Fact]
    public void Forecast_IntermittentSeriesEndingInZeros_IsNotAllZero()
    {
        // Caso observado (ALI-0001, TEC-0005): ventas cada 9 días y los últimos 8 días sin ventas.
        // Antes de centrar la serie, SSA devolvía 0 en todos los días.
        var series = Enumerable.Range(0, 200).Select(i => i % 9 == 0 ? 3f : 0f).Concat(Enumerable.Repeat(0f, 8)).ToList();

        var result = _engine.Forecast(series, 14);
        var total = result.Forecast.Sum(v => Math.Max(0, v));

        // El promedio histórico es ≈ 0,32 por día: se esperan unas 4 o 5 unidades en 14 días.
        Assert.InRange(total, 2.0, 8.0);
        Assert.All(result.Forecast, v => Assert.True(float.IsFinite(v)));
    }

    [Fact]
    public void Forecast_RecentZerosInFrequentProduct_StaysNearUsualLevel()
    {
        // Un producto que se vende a diario (~4 por día) con 7 días recientes en 0.
        var series = Enumerable.Range(0, 120).Select(i => 3f + i % 3).Concat(Enumerable.Repeat(0f, 7)).ToList();

        var result = _engine.Forecast(series, 7);

        Assert.True(result.Forecast.Average() > 1.5f, $"promedio estimado {result.Forecast.Average():F2}");
    }

    [Theory]
    [InlineData(90, 7)]
    [InlineData(365, 14)]
    public void Forecast_ReportsWindowUsed(int days, int expectedWindow)
    {
        var result = _engine.Forecast(WeeklySeries(days), 7);

        Assert.Equal(expectedWindow, result.WindowSize);
        Assert.True(result.SeriesLength > result.WindowSize);
        Assert.Equal(WeeklySeries(days).Average(), result.TrainingMean, 3);
    }
}
