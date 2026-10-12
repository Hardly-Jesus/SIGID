using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Interfaces.Services;

namespace SIGID.Web.Controllers;

/// <summary>
/// Dashboard con indicadores calculados a partir de los datos reales.
/// </summary>
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(cancellationToken);
        return View(summary);
    }
}
