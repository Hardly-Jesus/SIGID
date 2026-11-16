using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Configuration;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Evaluación de la precisión del modelo predictivo (HU-21). La evaluación la hace
/// IDemandForecastEvaluationService; aquí solo se reciben los filtros y se muestra el resultado.
/// </summary>
[Authorize]
public class ForecastEvaluationController : Controller
{
    private readonly IDemandForecastEvaluationService _evaluationService;
    private readonly IProductService _productService;
    private readonly DemandForecastOptions _options;

    public ForecastEvaluationController(IDemandForecastEvaluationService evaluationService, IProductService productService, DemandForecastOptions options)
    {
        _evaluationService = evaluationService;
        _productService = productService;
        _options = options;
    }

    // GET: /ForecastEvaluation?productId=1&dataSource=2&evaluationHorizon=14
    public async Task<IActionResult> Index(ForecastEvaluationViewModel model, CancellationToken cancellationToken)
    {
        model.Products = await _productService.GetAllAsync(cancellationToken);
        model.MinEvaluationDays = _options.MinEvaluationDays;
        model.MaxEvaluationDays = _options.MaxEvaluationDays;

        if (!Request.Query.Any())
        {
            model.EvaluationHorizon = _options.DefaultEvaluationDays;
            ModelState.Clear();
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Revisa los filtros: hay valores que no son válidos.";
            return View(model);
        }

        if (model.EvaluationHorizon is null)
        {
            model.ErrorMessage = $"Indica el período de evaluación, entre {_options.MinEvaluationDays} y {_options.MaxEvaluationDays} días.";
            return View(model);
        }

        var result = await _evaluationService.EvaluateAsync(model.ToRequest(), cancellationToken);
        model.Result = result;

        if (!result.IsSuccessful)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        model.Chart = DemandCharts.ForEvaluation(result);
        return View(model);
    }
}
