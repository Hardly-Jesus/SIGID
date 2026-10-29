using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Services;
using SIGID.Domain.Enums;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Entradas (HU-08), salidas (HU-09) e historial de movimientos (HU-10).
/// Las reglas de existencias se validan en InventoryService.
/// </summary>
[Authorize]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;

    public InventoryController(IInventoryService inventoryService, IProductService productService)
    {
        _inventoryService = inventoryService;
        _productService = productService;
    }

    // GET: /Inventory/Entries
    public async Task<IActionResult> Entries(CancellationToken cancellationToken)
    {
        var model = new MovementListViewModel
        {
            MovementType = MovementType.Entry,
            Movements = await _inventoryService.GetEntriesAsync(cancellationToken)
        };
        return View(model);
    }

    // GET: /Inventory/Exits
    public async Task<IActionResult> Exits(CancellationToken cancellationToken)
    {
        var model = new MovementListViewModel
        {
            MovementType = MovementType.Exit,
            Movements = await _inventoryService.GetExitsAsync(cancellationToken)
        };
        return View(model);
    }

    // GET: /Inventory/CreateEntry
    public async Task<IActionResult> CreateEntry(int? productId, CancellationToken cancellationToken)
    {
        return View(await NewFormAsync(MovementType.Entry, productId, cancellationToken));
    }

    // POST: /Inventory/CreateEntry
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEntry(MovementFormViewModel model, CancellationToken cancellationToken)
    {
        model.MovementType = MovementType.Entry;

        if (ModelState.IsValid)
        {
            try
            {
                var movement = await _inventoryService.RegisterEntryAsync(model.ToDto(), cancellationToken);
                TempData["SuccessMessage"] =
                    $"Entrada registrada: +{movement.Quantity} {movement.UnitOfMeasure} de {movement.ProductName}. " +
                    $"Existencia actual: {movement.StockAfter} {movement.UnitOfMeasure}.";
                return RedirectToAction(nameof(Entries));
            }
            catch (BusinessException ex)
            {
                ModelState.AddBusinessError(ex);
            }
        }

        model.Products = await _productService.GetActiveAsync(cancellationToken);
        return View(model);
    }

    // GET: /Inventory/CreateExit
    public async Task<IActionResult> CreateExit(int? productId, CancellationToken cancellationToken)
    {
        return View(await NewFormAsync(MovementType.Exit, productId, cancellationToken));
    }

    // POST: /Inventory/CreateExit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExit(MovementFormViewModel model, CancellationToken cancellationToken)
    {
        model.MovementType = MovementType.Exit;

        if (ModelState.IsValid)
        {
            try
            {
                var movement = await _inventoryService.RegisterExitAsync(model.ToDto(), cancellationToken);
                TempData["SuccessMessage"] =
                    $"Salida registrada: −{movement.Quantity} {movement.UnitOfMeasure} de {movement.ProductName}. " +
                    $"Existencia actual: {movement.StockAfter} {movement.UnitOfMeasure}.";
                return RedirectToAction(nameof(Exits));
            }
            catch (BusinessException ex)
            {
                // Aquí llega, entre otros, el aviso de existencias insuficientes.
                ModelState.AddBusinessError(ex);
            }
        }

        model.Products = await _productService.GetActiveAsync(cancellationToken);
        return View(model);
    }

    // GET: /Inventory/History?productId=1&type=Entry&from=2026-10-01&to=2026-10-08
    public async Task<IActionResult> History(int? productId, MovementType? type, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var model = new MovementHistoryViewModel
        {
            ProductId = productId,
            Type = type,
            From = from,
            To = to,
            Products = await _productService.GetAllAsync(cancellationToken)
        };

        try
        {
            model.Movements = await _inventoryService.GetHistoryAsync(model.ToFilter(), cancellationToken);
        }
        catch (BusinessException ex)
        {
            ModelState.AddBusinessError(ex);
        }

        return View(model);
    }

    private async Task<MovementFormViewModel> NewFormAsync(MovementType type, int? productId, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        return new MovementFormViewModel
        {
            MovementType = type,
            ProductId = productId ?? 0,
            // Fecha y hora actual, sin segundos, como valor sugerido.
            Date = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0),
            Products = await _productService.GetActiveAsync(cancellationToken)
        };
    }
}
