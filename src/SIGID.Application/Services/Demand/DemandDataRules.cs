using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Reglas para decidir si el historial de un producto permite pronosticar y evaluar (HU-19 / HU-21).
/// </summary>
/// <remarks>
/// - Días mínimos para entrenar = el mayor entre 56 (8 semanas), lo que exige ML.NET
///   (más del doble de la ventana) y el doble del horizonte pedido.
/// - Además se necesitan al menos MinimumDaysWithSales días con ventas.
/// - No es lo mismo un período sin ventas (días conocidos en 0) que un período sin información.
/// - Una serie con días sin información o con cantidades inválidas no se usa.
/// </remarks>
public static class DemandDataRules
{
    /// <summary>
    /// Revisa el historial de entrenamiento para pronosticar <paramref name="horizon"/> días.
    /// </summary>
    /// <param name="requestedDays">Días del período elegido por el usuario.</param>
    /// <param name="evaluationDays">Días que se reservarían para evaluar con este mismo historial.</param>
    public static DemandDataAvailabilityDto ForTraining(DemandSeriesDto series, DemandForecastOptions options, int horizon, int requestedDays, int evaluationDays)
    {
        var required = options.RequiredTrainingDaysFor(horizon);
        var requiredForEvaluation = options.RequiredTrainingDaysFor(evaluationDays) + evaluationDays;

        var availability = new DemandDataAvailabilityDto
        {
            ProductId = series.ProductId,
            DataSource = series.DataSource,
            ObservationCount = series.Points.Count,
            DaysWithSales = series.DaysWithSales,
            RequiredObservationCount = required,
            RequiredForEvaluationCount = requiredForEvaluation,
            RequestedDayCount = requestedDays,
            FirstKnownDate = series.StartDate
        };

        if (series.InvalidRecordCount > 0)
            return Set(availability, DemandDataStatus.InvalidData,
                "El historial de este producto tiene cantidades inválidas, por eso no se puede usar para pronosticar.");

        // El período elegido, aunque tuviera datos todos los días, no alcanza.
        if (requestedDays < required)
            return Set(availability, DemandDataStatus.InsufficientHistory,
                $"El período seleccionado es demasiado corto para generar una predicción confiable. " +
                $"Para estimar {horizon} {Days(horizon)} se necesitan al menos {required} días de historial y el período elegido tiene {requestedDays}. " +
                (required <= 92 ? "Prueba con los últimos 3 o 6 meses." : "Prueba con los últimos 6 o 12 meses, o con un horizonte más corto."));

        if (series.Points.Count == 0)
            return Set(availability, DemandDataStatus.NoHistory,
                "No hay información de ventas de este producto en el período seleccionado. Prueba con un período más amplio u otro origen de datos.");

        if (series.DaysWithSales == 0)
            return Set(availability, DemandDataStatus.NoHistory,
                $"Este producto no tuvo ventas en el período seleccionado ({series.Points.Count} días con información). Prueba con un período más amplio.");

        if (series.UnknownDayCount > 0)
            return Set(availability, DemandDataStatus.InvalidData,
                $"El historial tiene {series.UnknownDayCount} días sin información dentro del período. El modelo necesita días continuos: elige un período sin interrupciones.");

        // Hay información, pero empieza después del inicio del período elegido.
        if (series.Points.Count < required)
            return Set(availability, DemandDataStatus.InsufficientHistory,
                $"Solo hay información de ventas de este producto desde el {series.StartDate:dd/MM/yyyy} ({series.Points.Count} días). " +
                $"Para estimar {horizon} {Days(horizon)} se necesitan al menos {required}. Elige un horizonte más corto o un producto con más historial.");

        if (series.DaysWithSales < options.MinimumDaysWithSales)
            return Set(availability, DemandDataStatus.InsufficientHistory,
                $"El período seleccionado no contiene suficientes datos para generar una predicción: tiene {series.DaysWithSales} {Days(series.DaysWithSales)} con ventas " +
                $"y se necesitan al menos {options.MinimumDaysWithSales}. Prueba con un intervalo más amplio.");

        availability.CanForecast = true;

        // ¿Alcanzaría este mismo historial para reservar días y evaluar la precisión?
        var trainingPart = series.Points.Take(Math.Max(0, series.Points.Count - evaluationDays)).ToList();
        if (series.Points.Count < requiredForEvaluation || trainingPart.Count(p => p.Quantity > 0) < options.MinimumDaysWithSales)
            return Set(availability, DemandDataStatus.ForecastOnly,
                $"Se puede generar una predicción, pero este historial no alcanza para evaluar su precisión: " +
                $"se necesitan al menos {requiredForEvaluation} días ({requiredForEvaluation - evaluationDays} para entrenar y {evaluationDays} para evaluar).");

        availability.CanEvaluate = true;
        return Set(availability, DemandDataStatus.Evaluable, "Hay historial suficiente para pronosticar y para evaluar la precisión.");
    }

    /// <summary>
    /// Disponibilidad general con el horizonte por defecto (usada para consultar el estado de un producto).
    /// </summary>
    public static DemandDataAvailabilityDto Evaluate(DemandSeriesDto series, DemandForecastOptions options, int evaluationDays)
        => ForTraining(series, options, options.DefaultHorizonDays, series.Points.Count + series.UnknownDayCount + series.InvalidRecordCount, evaluationDays);

    private static string Days(int count) => count == 1 ? "día" : "días";

    private static DemandDataAvailabilityDto Set(DemandDataAvailabilityDto availability, DemandDataStatus status, string message)
    {
        availability.Status = status;
        availability.Message = message;
        return availability;
    }
}
