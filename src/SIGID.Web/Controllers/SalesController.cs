using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Application.DTOs.Sales;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Registro de ventas (HU-14 / HU-15) e historial de ventas (HU-16).
/// Las reglas de la venta y la actualización de existencias están en SaleService.
/// </summary>
[Authorize]
public class SalesController : Controller
{
    private readonly ISaleService _saleService;
    private readonly IProductService _productService;

    public SalesController(ISaleService saleService, IProductService productService)
    {
        _saleService = saleService;
        _productService = productService;
    }

    // GET: /Sales?productId=1&from=2026-10-01&to=2026-10-20
    public async Task<IActionResult> Index(int? productId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var model = new SaleHistoryViewModel
        {
            ProductId = productId,
            From = from,
            To = to,
            Products = await _productService.GetAllAsync(cancellationToken)
        };

        try
        {
            model.Sales = await _saleService.GetHistoryAsync(model.ToFilter(), cancellationToken);
        }
        catch (BusinessException ex)
        {
            ModelState.AddBusinessError(ex);
        }

        return View(model);
    }

    // GET: /Sales/Details/5
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var sale = await _saleService.GetByIdAsync(id, cancellationToken);
        if (sale is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound", id);
        }

        return View(sale);
    }

    // GET: /Sales/Create
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new RegisterSaleViewModel { RequestId = Guid.NewGuid() };
        await LoadProductsAsync(model, cancellationToken);
        return View(model);
    }

    // POST: /Sales/Create
    // El mismo formulario tiene tres botones: agregar producto, quitar línea y confirmar la venta.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterSaleViewModel model, string? command, CancellationToken cancellationToken)
    {
        if (model.RequestId == Guid.Empty)
            model.RequestId = Guid.NewGuid();

        await LoadProductsAsync(model, cancellationToken);

        if (command == "confirm")
            return await ConfirmAsync(model, cancellationToken);

        // Agregar o quitar líneas solo cambia el formulario; todavía no se guarda nada.
        ModelState.Clear();

        if (command?.StartsWith("remove:") == true && int.TryParse(command["remove:".Length..], out var index)
            && index >= 0 && index < model.Lines.Count)
            model.Lines.RemoveAt(index);

        // Las cantidades de las líneas pudieron cambiar: se revisan antes de agregar otra.
        await ValidateLinesAsync(model, cancellationToken);

        if (command == "add")
            await AddLineAsync(model, cancellationToken);

        return View(model);
    }

    private async Task<IActionResult> ConfirmAsync(RegisterSaleViewModel model, CancellationToken cancellationToken)
    {
        // Los campos de "agregar producto" no forman parte de la confirmación.
        ModelState.Remove(nameof(model.NewProductId));
        ModelState.Remove(nameof(model.NewQuantity));

        // Las validaciones de cada línea se hacen abajo con mensajes más claros.
        for (var i = 0; i < model.Lines.Count; i++)
            ModelState.Remove($"Lines[{i}].Quantity");

        if (model.Lines.Count == 0)
        {
            ShowError(model, "Revisa la venta.", "Agrega al menos un producto antes de confirmar la venta.");
            return View(model);
        }

        // Revisión rápida para avisar sin intentar guardar.
        if (!await ValidateLinesAsync(model, cancellationToken))
            return View(model);

        // Validación definitiva: SaleService vuelve a comprobar todo dentro de la transacción.
        var result = await _saleService.RegisterAsync(model.ToDto(), cancellationToken);

        if (!result.Succeeded)
        {
            // La venta se rechazó completa y no se guardó nada; el formulario conserva los datos.
            var title = result.Errors.Any(e => e.IsStockError)
                ? "No hay suficientes existencias."
                : "No se pudo registrar la venta.";
            ShowError(model, title, result.Errors.Select(e => e.Message).ToArray());
            return View(model);
        }

        var sale = result.Sale!;
        TempData["SaleTitle"] = result.WasAlreadyRegistered
            ? "Esta venta ya estaba registrada."
            : "¡Venta registrada exitosamente!";
        TempData["SaleMessage"] = result.WasAlreadyRegistered
            ? $"La venta #{sale.Id} ya se había guardado antes, así que las existencias no se descontaron otra vez."
            : $"La venta #{sale.Id} se guardó correctamente y las existencias del inventario se actualizaron automáticamente.";

        return RedirectToAction(nameof(Details), new { id = sale.Id });
    }

    private async Task AddLineAsync(RegisterSaleViewModel model, CancellationToken cancellationToken)
    {
        var product = model.Products.FirstOrDefault(p => p.Id == model.NewProductId);

        if (product is null)
            ModelState.AddModelError(nameof(model.NewProductId), "Selecciona un producto.");

        if (model.NewQuantity is null or <= 0)
            ModelState.AddModelError(nameof(model.NewQuantity), "Escribe una cantidad mayor que cero.");

        if (product is null || model.NewQuantity is null or <= 0)
            return;

        var existing = model.Lines.FirstOrDefault(l => l.ProductId == product.Id);
        var alreadyInSale = existing?.Quantity ?? 0;

        // Se valida la cantidad nueva (sumada a la que ya está en la venta) contra las existencias reales.
        var problem = await _saleService.CheckAvailabilityAsync(product.Id, model.NewQuantity.Value, alreadyInSale, cancellationToken: cancellationToken);
        if (problem is not null)
        {
            // No se agrega: se conservan el producto y la cantidad elegidos para que se puedan corregir.
            ModelState.AddModelError(nameof(model.NewQuantity), "Supera las existencias.");
            ShowError(model, problem.IsStockError ? "No hay suficientes existencias." : "Revisa el producto.", problem.Message);
            return;
        }

        if (existing is not null)
        {
            // Producto repetido: se suma la cantidad en la línea que ya existe.
            existing.Quantity = alreadyInSale + model.NewQuantity.Value;
            ViewData["InfoMessage"] = $"«{product.Name}» ya estaba en la venta; se sumó la cantidad (total: {existing.Quantity} {product.UnitOfMeasure}).";
        }
        else
        {
            model.Lines.Add(new RegisterSaleDetailViewModel
            {
                ProductId = product.Id,
                Quantity = model.NewQuantity,
                Product = product
            });
        }

        model.NewProductId = null;
        model.NewQuantity = null;
    }

    // Revisa la cantidad de cada línea (por si se modificó a mano). Devuelve false si hay problemas.
    private async Task<bool> ValidateLinesAsync(RegisterSaleViewModel model, CancellationToken cancellationToken)
    {
        var messages = new List<string>();
        var stockProblem = false;

        for (var i = 0; i < model.Lines.Count; i++)
        {
            var line = model.Lines[i];
            var key = $"Lines[{i}].Quantity";

            if (line.Product is null)
            {
                messages.Add("Un producto de la venta ya no está disponible. Quítalo de la venta.");
                continue;
            }

            if (line.Quantity is null or <= 0)
            {
                ModelState.AddModelError(key, "Escribe una cantidad mayor que cero.");
                messages.Add($"Escribe una cantidad mayor que cero para «{line.Product.Name}».");
                continue;
            }

            var problem = await _saleService.CheckAvailabilityAsync(line.ProductId, line.Quantity.Value, isExistingLine: true, cancellationToken: cancellationToken);
            if (problem is not null)
            {
                ModelState.AddModelError(key, problem.IsStockError
                    ? (line.Product.CurrentStock > 0 ? $"Solo hay {line.Product.CurrentStock} {line.Product.UnitOfMeasure} disponibles." : "No hay existencias disponibles.")
                    : problem.Message);
                messages.Add(problem.Message);
                stockProblem |= problem.IsStockError;
            }
        }

        if (messages.Count == 0)
            return true;

        ShowError(model, stockProblem ? "No hay suficientes existencias." : "Revisa la venta.", messages.ToArray());
        return false;
    }

    // Aviso que se muestra junto al formulario, con un título y uno o más mensajes.
    private static void ShowError(RegisterSaleViewModel model, string title, params string[] messages)
    {
        model.ErrorTitle ??= title;
        model.ErrorMessages.AddRange(messages.Where(m => !model.ErrorMessages.Contains(m)));
    }

    private async Task LoadProductsAsync(RegisterSaleViewModel model, CancellationToken cancellationToken)
    {
        model.Products = await _productService.GetActiveAsync(cancellationToken);

        // Los datos de cada línea se toman de la base de datos, no del formulario.
        foreach (var line in model.Lines)
            line.Product = model.Products.FirstOrDefault(p => p.Id == line.ProductId);
    }
}
