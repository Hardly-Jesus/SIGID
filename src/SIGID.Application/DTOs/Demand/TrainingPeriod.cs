namespace SIGID.Application.DTOs.Demand;

/// <summary>
/// Historial que se usa para entrenar el modelo. Los períodos relativos se cuentan hacia atrás
/// desde el último día de entrenamiento permitido (ayer en la predicción; el día anterior al
/// período evaluado en la evaluación).
/// </summary>
public enum TrainingPeriod
{
    LastMonth = 1,
    Last3Months = 2,
    Last6Months = 3,
    Last12Months = 4,
    AllHistory = 5,
    Custom = 6
}
