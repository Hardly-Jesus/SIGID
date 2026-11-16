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
    public async Task<IActionResult> Entries(int page = 1, CancellationToken cancellationToken = default)
    {
        var model = new MovementListViewModel
        {
            MovementType = MovementType.Entry
        };
        await LoadPageAsync(model, page, nameof(Entries), cancellationToken);
        return View(model);
    }

    // GET: /Inventory/Exits
    public async Task<IActionResult> Exits(int page = 1, CancellationToken cancellationToken = default)
    {
        var model = new MovementListViewModel
        {
            MovementType = MovementType.Exit
        };
        await LoadPageAsync(model, page, nameof(Exits), cancellationToken);
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
    public async Task<IActionResult> History(int? productId, MovementType? type, DateTime? from, DateTime? to, int page = 1, CancellationToken cancellationToken = default)
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
            // Solo se carga la página visible; los totales vienen calculados de la base de datos.
            var result = await _inventoryService.GetHistoryPageAsync(model.ToFilter(), page, PagerViewModel.DefaultPageSize, cancellationToken);
            model.Movements = result.Items;
            model.TotalCount = result.TotalCount;
            model.TotalEntries = result.TotalEntries;
            model.TotalExits = result.TotalExits;
            model.Pager = new PagerViewModel
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                ItemsName = "movimientos",
                Action = nameof(History),
                RouteValues = new()
                {
                    ["productId"] = productId?.ToString(),
                    ["type"] = type?.ToString(),
                    ["from"] = from?.ToString("yyyy-MM-dd"),
                    ["to"] = to?.ToString("yyyy-MM-dd")
                }
            };
        }
        catch (BusinessException ex)
        {
            ModelState.AddBusinessError(ex);
        }

        return View(model);
    }

    // Carga una página de entradas o de salidas.
    private async Task LoadPageAsync(MovementListViewModel model, int page, string action, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetMovementsPageAsync(model.MovementType, page, PagerViewModel.DefaultPageSize, cancellationToken);
        model.Movements = result.Items;
        model.TotalCount = result.TotalCount;
        model.Pager = new PagerViewModel
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            ItemsName = model.IsEntry ? "entradas" : "salidas",
            Action = action
        };
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
