using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Configuration;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Herramienta de desarrollo para generar datos históricos de prueba (HU-17).
/// Solo existe cuando SyntheticData:Enabled es true y la aplicación corre en Development;
/// en cualquier otro caso responde 404. Requiere sesión iniciada.
/// </summary>
[Authorize]
public class SyntheticDataController : Controller
{
    private readonly ISyntheticSalesService _syntheticService;
    private readonly IProductService _productService;
    private readonly SyntheticDataOptions _options;
    private readonly TimeProvider _timeProvider;

    public SyntheticDataController(ISyntheticSalesService syntheticService, IProductService productService, SyntheticDataOptions options, TimeProvider timeProvider)
    {
        _syntheticService = syntheticService;
        _productService = productService;
        _options = options;
        _timeProvider = timeProvider;
    }

    // GET: /SyntheticData
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_syntheticService.IsGenerationEnabled)
            return NotFound();

        // Propuesta inicial: los últimos 12 meses hasta ayer.
        var yesterday = _timeProvider.GetLocalNow().Date.AddDays(-1);
        var model = new SyntheticDataViewModel
        {
            EndDate = yesterday,
            StartDate = yesterday.AddMonths(-12).AddDays(1)
        };

        await LoadAsync(model, cancellationToken);
        return View(model);
    }

    // POST: /SyntheticData/Generate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(SyntheticDataViewModel model, CancellationToken cancellationToken)
    {
        if (!_syntheticService.IsGenerationEnabled)
            return NotFound();

        if (!ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Revisa los datos del formulario: hay valores que no son válidos.");
            await LoadAsync(model, cancellationToken);
            return View("Index", model);
        }

        var result = await _syntheticService.GenerateAsync(model.ToDto(), cancellationToken);
        if (!result.IsSuccessful)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudieron generar los datos de prueba.");
            await LoadAsync(model, cancellationToken);
            return View("Index", model);
        }

        TempData["SuccessMessage"] = $"Los datos históricos se generaron correctamente. Lote #{result.BatchId}: {result.GeneratedRecords:N0} registros para {result.ProductCount} producto(s), del {result.StartDate:dd/MM/yyyy} al {result.EndDate:dd/MM/yyyy}.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /SyntheticData/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!_syntheticService.IsGenerationEnabled)
            return NotFound();

        if (await _syntheticService.DeleteBatchAsync(id, cancellationToken))
            TempData["SuccessMessage"] = $"Se eliminó el lote #{id} y todos sus registros de prueba.";
        else
            TempData["ErrorMessage"] = "El lote seleccionado no existe o ya fue eliminado.";

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadAsync(SyntheticDataViewModel model, CancellationToken cancellationToken)
    {
        model.Products = await _productService.GetAllAsync(cancellationToken);
        model.Batches = await _syntheticService.GetBatchesAsync(cancellationToken);
        model.MaxProducts = _options.MaxProductsPerBatch;
    }
}
