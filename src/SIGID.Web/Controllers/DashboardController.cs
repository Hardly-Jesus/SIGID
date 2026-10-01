using Microsoft.AspNetCore.Mvc;

namespace SIGID.Web.Controllers;

/// <summary>
/// Dashboard base. Los indicadores reales se implementarán en sprints posteriores.
/// </summary>
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
