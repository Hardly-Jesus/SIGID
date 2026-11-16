using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Análisis histórico de ventas por producto (HU-18). Solo consulta: no modifica ventas ni existencias.
/// </summary>
[Authorize]
public class SalesAnalysisController : Controller
{
    private readonly ISalesAnalysisService _analysisService;
    private readonly IProductService _productService;
    private readonly TimeProvider _timeProvider;

    public SalesAnalysisController(ISalesAnalysisService analysisService, IProductService productService, TimeProvider timeProvider)
    {
        _analysisService = analysisService;
        _productService = productService;
        _timeProvider = timeProvider;
    }

    // GET: /SalesAnalysis?productId=1&startDate=2026-01-01&endDate=2026-10-31&groupBy=2&dataSource=1
    public async Task<IActionResult> Index(SalesAnalysisViewModel model, CancellationToken cancellationToken)
    {
        model.Products = await _productService.GetAllAsync(cancellationToken);

        // Primera visita: se proponen los últimos 3 meses y todavía no se consulta.
        if (!Request.Query.Any())
        {
            var today = _timeProvider.GetLocalNow().Date;
            model.EndDate = today;
            model.StartDate = today.AddMonths(-3).AddDays(1);
            ModelState.Clear();
            return View(model);
        }

        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Revisa los filtros: hay valores que no son válidos.";
            return View(model);
        }

        var result = await _analysisService.AnalyzeAsync(model.ToFilter(), cancellationToken);
        if (!result.IsSuccessful)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        model.Result = result;
        if (result.Periods.Count > 0)
            model.Chart = DemandCharts.ForAnalysis(result);

        return View(model);
    }
}
