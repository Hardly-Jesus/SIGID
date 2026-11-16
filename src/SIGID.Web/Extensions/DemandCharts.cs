using SIGID.Application.DTOs.Demand;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Extensions;

/// <summary>
/// Prepara los datos de los gráficos a partir de los resultados de los servicios.
/// Solo reordena valores para dibujarlos; no los modifica.
/// </summary>
public static class DemandCharts
{
    /// <summary>
    /// Gráfico del análisis histórico (HU-18): una línea con las cantidades de cada período.
    /// </summary>
    public static DemandChartViewModel ForAnalysis(SalesAnalysisResultDto result)
    {
        return new DemandChartViewModel
        {
            Description = $"Evolución de las ventas de {result.ProductName} {DemandFormat.GroupingUnit(result.GroupBy)}.",
            Labels = result.Periods.Select(p => DemandFormat.ChartLabel(p.Period, result.GroupBy)).ToList(),
            Series =
            {
                new DemandChartSeries
                {
                    Name = "Cantidad vendida",
                    Kind = DemandChartSeriesKind.History,
                    Values = result.Periods.Select(p => (double?)p.TotalQuantity).ToArray()
                }
            }
        };
    }

    /// <summary>
    /// Gráfico de la predicción (HU-20): historial reciente y, a continuación, el pronóstico.
    /// </summary>
    public static DemandChartViewModel ForForecast(DemandForecastViewModel model)
    {
        var history = model.HistoricalPeriods;
        var forecast = model.ForecastPeriods;

        // Eje de fechas continuo: si entre el historial y el pronóstico queda un día (hoy,
        // que todavía no termina), ese día aparece en el eje sin valor.
        var firstDate = history.Count > 0 ? history[0].Date : forecast[0].Date;
        var lastDate = forecast[^1].Date;
        var total = (lastDate - firstDate).Days + 1;

        var labels = new List<string>();
        var historyValues = new double?[total];
        var forecastValues = new double?[total];
        var lower = new double?[total];
        var upper = new double?[total];

        for (var i = 0; i < total; i++)
            labels.Add(DemandFormat.ShortDate(firstDate.AddDays(i)));

        foreach (var point in history)
            historyValues[(point.Date - firstDate).Days] = point.Quantity;

        foreach (var period in forecast)
        {
            var index = (period.Date - firstDate).Days;
            forecastValues[index] = period.PredictedQuantity;
            if (model.HasPredictionBounds)
            {
                lower[index] = period.LowerBound;
                upper[index] = period.UpperBound;
            }
        }

        return new DemandChartViewModel
        {
            Description = $"Ventas recientes y demanda estimada de {model.ProductName} del {DemandFormat.Date(model.ForecastStartDate)} al {DemandFormat.Date(model.ForecastEndDate)}.",
            Labels = labels,
            Series =
            {
                new DemandChartSeries { Name = "Ventas históricas", Kind = DemandChartSeriesKind.History, Values = historyValues },
                new DemandChartSeries { Name = "Demanda estimada", Kind = DemandChartSeriesKind.Forecast, Values = forecastValues }
            },
            LowerBound = model.HasPredictionBounds ? lower : null,
            UpperBound = model.HasPredictionBounds ? upper : null,
            BandName = "Rango estimado (no garantizado)",
            DividerIndex = history.Count > 0 ? (forecast[0].Date - firstDate).Days : null,
            DividerLabel = "Inicio del pronóstico"
        };
    }

    /// <summary>
    /// Gráfico de la evaluación (HU-21): cantidades reales frente a las estimadas.
    /// </summary>
    public static DemandChartViewModel ForEvaluation(DemandForecastEvaluationResultDto result)
    {
        var periods = result.EvaluationPeriods;
        return new DemandChartViewModel
        {
            Description = $"Comparación entre las ventas reales y las estimadas de {result.ProductName} del {DemandFormat.Date(result.EvaluationStartDate)} al {DemandFormat.Date(result.EvaluationEndDate)}.",
            Labels = periods.Select(p => DemandFormat.ShortDate(p.Date)).ToList(),
            Series =
            {
                new DemandChartSeries { Name = "Cantidad real", Kind = DemandChartSeriesKind.Actual, Values = periods.Select(p => (double?)p.ActualQuantity).ToArray() },
                new DemandChartSeries { Name = "Estimación SSA", Kind = DemandChartSeriesKind.Forecast, Values = periods.Select(p => (double?)p.PredictedQuantity).ToArray() },
                new DemandChartSeries { Name = "Referencia (último valor)", Kind = DemandChartSeriesKind.Reference, Values = periods.Select(p => (double?)p.BaselineQuantity).ToArray() }
            }
        };
    }
}
