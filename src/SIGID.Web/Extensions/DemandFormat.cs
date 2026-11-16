using System.Globalization;
using SIGID.Application.DTOs.Demand;
using SIGID.Domain.Enums;

namespace SIGID.Web.Extensions;

/// <summary>
/// Textos y formatos de las pantallas de demanda (EP-05).
/// </summary>
public static class DemandFormat
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es");

    /// <summary>
    /// Cantidad con hasta 2 decimales, por ejemplo "1.234,5".
    /// </summary>
    public static string Quantity(double value) => value.ToString("#,##0.##", Spanish);

    /// <summary>
    /// Número entero con separador de miles en español, por ejemplo "7.600".
    /// </summary>
    public static string Count(int value) => value.ToString("#,##0", Spanish);

    /// <summary>
    /// Cantidad estimada por el modelo, siempre con 2 decimales, por ejemplo "10,50".
    /// </summary>
    public static string Estimate(double value) => value.ToString("#,##0.00", Spanish);

    /// <summary>
    /// Número de una métrica con 2 decimales.
    /// </summary>
    public static string Metric(double? value) => value is null ? "No disponible" : value.Value.ToString("#,##0.00", Spanish);

    public static string Percent(double? value) => value is null ? "No disponible" : value.Value.ToString("#,##0.00", Spanish) + "\u00A0%";

    public static string Date(DateTime? date) => date?.ToString("dd/MM/yyyy") ?? "—";

    public static string ShortDate(DateTime date) => date.ToString("dd/MM");

    public static string DayName(DateTime date) => Spanish.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek).TrimEnd('.');

    public static string Source(SalesDataSource source) => source == SalesDataSource.Synthetic ? "Datos de prueba" : "Ventas reales";

    public static string TrainingPeriodName(TrainingPeriod period) => period switch
    {
        TrainingPeriod.LastMonth => "Último mes",
        TrainingPeriod.Last3Months => "Últimos 3 meses",
        TrainingPeriod.Last6Months => "Últimos 6 meses",
        TrainingPeriod.Last12Months => "Últimos 12 meses",
        TrainingPeriod.AllHistory => "Todo el historial",
        _ => "Personalizado"
    };

    public static string Behavior(DemandBehavior behavior) => behavior switch
    {
        DemandBehavior.Frequent => "Ventas frecuentes",
        DemandBehavior.Moderate => "Ventas moderadas",
        DemandBehavior.Sporadic => "Ventas esporádicas",
        _ => "Historial insuficiente"
    };

    public static string Pattern(DemandPattern pattern) => pattern switch
    {
        DemandPattern.Increasing => "Creciente",
        DemandPattern.Decreasing => "Decreciente",
        DemandPattern.Seasonal => "Estacional",
        _ => "Estable"
    };

    /// <summary>
    /// Nombre del período agrupado: día, semana (desde el lunes) o mes.
    /// </summary>
    public static string Period(DateTime period, SalesGrouping groupBy) => groupBy switch
    {
        SalesGrouping.Week => $"Semana del {period:dd/MM/yyyy}",
        SalesGrouping.Month => Capitalize(period.ToString("MMMM yyyy", Spanish)),
        _ => period.ToString("dd/MM/yyyy")
    };

    public static string ChartLabel(DateTime period, SalesGrouping groupBy) => groupBy switch
    {
        SalesGrouping.Month => Capitalize(period.ToString("MMM yy", Spanish).Replace(".", "")),
        SalesGrouping.Week => period.ToString("dd/MM"),
        _ => period.ToString("dd/MM")
    };

    public static string GroupingUnit(SalesGrouping groupBy) => groupBy switch
    {
        SalesGrouping.Week => "por semana",
        SalesGrouping.Month => "por mes",
        _ => "por día"
    };

    public static string Trend(SalesTrend trend) => trend switch
    {
        SalesTrend.Increasing => "Creciente",
        SalesTrend.Decreasing => "Decreciente",
        SalesTrend.Stable => "Estable",
        _ => "Sin datos suficientes"
    };

    public static string Status(DemandDataStatus status) => status switch
    {
        DemandDataStatus.NoHistory => "Sin historial",
        DemandDataStatus.InsufficientHistory => "Historial insuficiente",
        DemandDataStatus.InvalidData => "Datos inválidos",
        DemandDataStatus.ForecastOnly => "Predicción disponible sin evaluación suficiente",
        _ => "Evaluación disponible"
    };

    private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpper(text[0], Spanish) + text[1..];
}
