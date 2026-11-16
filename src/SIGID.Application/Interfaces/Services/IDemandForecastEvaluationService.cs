using SIGID.Application.DTOs.Demand;

namespace SIGID.Application.Interfaces.Services;

/// <summary>
/// Evaluación de la precisión del modelo predictivo (HU-21).
/// </summary>
public interface IDemandForecastEvaluationService
{
    Task<DemandForecastEvaluationResultDto> EvaluateAsync(DemandForecastEvaluationRequestDto request, CancellationToken cancellationToken = default);
}
