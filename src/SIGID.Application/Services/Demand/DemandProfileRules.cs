using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Clasifica el comportamiento de las ventas según la proporción de días con ventas.
/// </summary>
/// <remarks>
/// Reglas (configurables en appsettings):
/// - Ventas frecuentes: hay ventas en al menos el 60 % de los días.
/// - Ventas moderadas: entre el 25 % y el 60 %.
/// - Ventas esporádicas: menos del 25 %.
/// - Historial insuficiente: cuando el historial no alcanza para entrenar el modelo.
/// Además se avisa si el producto no tuvo ventas en los últimos 28 días (dejó de venderse).
/// </remarks>
public static class DemandProfileRules
{
    public static DemandProfileDto Classify(IReadOnlyList<DailyDemandPointDto> points, DemandForecastOptions options, bool hasEnoughHistory)
    {
        var profile = new DemandProfileDto
        {
            ObservedDays = points.Count,
            DaysWithSales = points.Count(p => p.Quantity > 0),
            TotalQuantity = points.Sum(p => p.Quantity)
        };

        if (points.Count > 0)
        {
            profile.ZeroDayPercent = Math.Round((points.Count - profile.DaysWithSales) * 100.0 / points.Count, 1);
            profile.AverageDailyQuantity = Math.Round(profile.TotalQuantity / points.Count, 2);
        }

        if (profile.DaysWithSales > 0)
            profile.AverageQuantityWhenSold = Math.Round(profile.TotalQuantity / profile.DaysWithSales, 2);

        // Días seguidos sin ventas al final del historial.
        for (var i = points.Count - 1; i >= 0 && points[i].Quantity == 0; i--)
            profile.TrailingDaysWithoutSales++;

        profile.StoppedSelling = profile.DaysWithSales > 0 && profile.TrailingDaysWithoutSales >= options.InactiveDaysWarning;

        if (!hasEnoughHistory || points.Count == 0)
        {
            profile.Behavior = DemandBehavior.InsufficientHistory;
            return profile;
        }

        var share = (double)profile.DaysWithSales / points.Count;
        profile.Behavior = share >= options.FrequentSalesMinShare ? DemandBehavior.Frequent
            : share >= options.ModerateSalesMinShare ? DemandBehavior.Moderate
            : DemandBehavior.Sporadic;
        return profile;
    }
}
