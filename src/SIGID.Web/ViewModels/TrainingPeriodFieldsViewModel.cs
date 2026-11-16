using SIGID.Application.DTOs.Demand;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Datos del selector «Historial de entrenamiento» (compartido por Predicción y Precisión del modelo).
/// </summary>
public class TrainingPeriodFieldsViewModel
{
    public TrainingPeriod TrainingPeriod { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Texto de ayuda bajo las fechas personalizadas.
    /// </summary>
    public string? Hint { get; set; }
}
