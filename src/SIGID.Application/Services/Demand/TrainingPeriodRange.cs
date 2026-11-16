using SIGID.Application.Configuration;
using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Convierte la opción «Historial de entrenamiento» en fechas concretas (predicción y evaluación).
/// </summary>
public static class TrainingPeriodRange
{
    /// <param name="lastAllowedDay">Último día que puede entrar al entrenamiento.</param>
    /// <param name="today">Hoy: sus ventas todavía no están completas.</param>
    public static (DateTime Start, DateTime End, string? Error) Resolve(
        TrainingPeriod period, DateTime? customStart, DateTime? customEnd,
        DateTime lastAllowedDay, DateTime today, DemandForecastOptions options)
    {
        if (!Enum.IsDefined(period))
            return Fail("Selecciona un historial de entrenamiento válido.");

        if (period != TrainingPeriod.Custom)
        {
            // Los períodos relativos terminan en el último día permitido.
            var end = lastAllowedDay.Date;
            var start = period switch
            {
                TrainingPeriod.LastMonth => end.AddMonths(-1).AddDays(1),
                TrainingPeriod.Last3Months => end.AddMonths(-3).AddDays(1),
                TrainingPeriod.Last6Months => end.AddMonths(-6).AddDays(1),
                TrainingPeriod.Last12Months => end.AddMonths(-12).AddDays(1),
                _ => end.AddDays(-(options.MaxTrainingDays - 1))     // todo el historial (con un máximo)
            };
            return (start, end, null);
        }

        if (customStart is null || customEnd is null)
            return Fail("Indica las fechas Desde y Hasta del historial personalizado.");

        var from = customStart.Value.Date;
        var to = customEnd.Value.Date;

        if (from > to)
            return Fail("La fecha inicial no puede ser posterior a la fecha final. Revisa el período seleccionado.");

        if (to >= today.Date)
            return Fail("El historial de entrenamiento no puede incluir el día de hoy ni fechas futuras, porque las ventas de hoy todavía no están completas.");

        if (to > lastAllowedDay.Date)
            return Fail($"La fecha final del historial no puede ser posterior al {lastAllowedDay:dd/MM/yyyy}.");

        if ((to - from).Days + 1 > options.MaxTrainingDays)
            return Fail($"El historial personalizado no puede superar {options.MaxTrainingDays} días (3 años). Elige un período más corto.");

        return (from, to, null);
    }

    private static (DateTime, DateTime, string?) Fail(string message) => (default, default, message);
}
