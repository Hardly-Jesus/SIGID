using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Services.Demand;

/// <summary>
/// Divide una serie diaria en entrenamiento y evaluación respetando el orden del tiempo (HU-21).
/// Los últimos días se reservan para evaluar; nunca se usan particiones aleatorias.
/// </summary>
public static class ChronologicalSplit
{
    public static (List<DailyDemandPointDto> Training, List<DailyDemandPointDto> Evaluation) Split(
        IReadOnlyList<DailyDemandPointDto> points, int evaluationDays, int minimumTrainingDays)
    {
        if (evaluationDays <= 0)
            throw new ArgumentException("El período de evaluación debe tener al menos un día.");

        // La serie debe estar ordenada y sin días repetidos ni saltos.
        for (var i = 1; i < points.Count; i++)
        {
            if (points[i].Date != points[i - 1].Date.AddDays(1))
                throw new ArgumentException("La serie no es continua: hay fechas desordenadas, repetidas o faltantes.");
        }

        if (points.Count - evaluationDays < minimumTrainingDays)
            throw new ArgumentException("No hay suficientes días para entrenar y evaluar.");

        // Separamos los últimos días para evaluar la predicción.
        var cut = points.Count - evaluationDays;
        return (points.Take(cut).ToList(), points.Skip(cut).ToList());
    }
}
