using SIGID.Application.DTOs.Demand;
using SIGID.Application.Services.Demand;

namespace SIGID.Application.Tests;

public class ForecastMetricsTests
{
    [Fact]
    public void Calculate_KnownValues()
    {
        // Errores: 1, -2, 0, 3 → MAE = 6/4 = 1,5; RMSE = √(14/4) ≈ 1,8708; WAPE = 6/20 × 100 = 30 %
        var actual = new double[] { 5, 3, 7, 5 };
        var predicted = new double[] { 4, 5, 7, 2 };

        var metrics = ForecastMetrics.Calculate(actual, predicted);

        Assert.Equal(1.5, metrics.MAE, 6);
        Assert.Equal(Math.Sqrt(3.5), metrics.RMSE, 6);
        Assert.Equal(30, metrics.WAPE!.Value, 6);
    }

    [Fact]
    public void Calculate_PerfectForecast_IsZero()
    {
        var metrics = ForecastMetrics.Calculate(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 });

        Assert.Equal(0, metrics.MAE);
        Assert.Equal(0, metrics.RMSE);
        Assert.Equal(0, metrics.WAPE);
    }

    [Fact]
    public void Calculate_WithoutRealSales_WapeIsNotAvailable()
    {
        var metrics = ForecastMetrics.Calculate(new double[] { 0, 0, 0 }, new double[] { 1, 0, 2 });

        Assert.Null(metrics.WAPE);
        Assert.Equal(1, metrics.MAE, 6);
    }

    [Fact]
    public void Calculate_InvalidInput_Throws()
    {
        Assert.Throws<ArgumentException>(() => ForecastMetrics.Calculate(new double[] { 1, 2 }, new double[] { 1 }));
        Assert.Throws<ArgumentException>(() => ForecastMetrics.Calculate(Array.Empty<double>(), Array.Empty<double>()));
        Assert.Throws<ArgumentException>(() => ForecastMetrics.Calculate(new double[] { 1 }, new[] { double.NaN }));
        Assert.Throws<ArgumentException>(() => ForecastMetrics.Calculate(new double[] { 1 }, new[] { double.PositiveInfinity }));
    }

    [Fact]
    public void NaiveForecast_RepeatsLastTrainingValue()
    {
        Assert.Equal(new double[] { 7, 7, 7 }, ForecastMetrics.NaiveForecast(new double[] { 1, 4, 7 }, 3));
    }

    [Theory]
    [InlineData(1.0, 2.0, ModelComparison.SsaBetter)]
    [InlineData(2.0, 1.0, ModelComparison.BaselineBetter)]
    [InlineData(1.5, 1.501, ModelComparison.Equal)]
    public void Compare_UsesMae(double ssa, double baseline, ModelComparison expected)
    {
        Assert.Equal(expected, ForecastMetrics.Compare(ssa, baseline));
    }
}

public class ChronologicalSplitTests
{
    private static List<DailyDemandPointDto> Series(int days)
        => Enumerable.Range(0, days).Select(i => new DailyDemandPointDto { Date = new DateTime(2026, 1, 1).AddDays(i), Quantity = i }).ToList();

    [Fact]
    public void Split_ReservesLastDays_WithoutOverlap()
    {
        var (training, evaluation) = ChronologicalSplit.Split(Series(50), 14, 28);

        Assert.Equal(36, training.Count);
        Assert.Equal(14, evaluation.Count);
        Assert.True(training.Last().Date < evaluation.First().Date);
        Assert.Equal(training.Last().Date.AddDays(1), evaluation.First().Date);
        Assert.Equal(new DateTime(2026, 2, 19), evaluation.Last().Date);
    }

    [Fact]
    public void Split_NotEnoughTrainingDays_Throws()
    {
        Assert.Throws<ArgumentException>(() => ChronologicalSplit.Split(Series(40), 14, 28));
    }

    [Fact]
    public void Split_UnorderedOrDuplicatedDates_Throws()
    {
        var series = Series(50);
        series[10].Date = series[9].Date;   // día repetido

        Assert.Throws<ArgumentException>(() => ChronologicalSplit.Split(series, 14, 28));
    }
}
