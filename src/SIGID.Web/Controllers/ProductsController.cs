using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Gestión de productos (HU-05).
/// Flujo: View → Controller → Service (Application) → Repository (Infrastructure) → SQL Server.
/// </summary>
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly ISupplierService _supplierService;

    public ProductsController(
        IProductService productService,
        ICategoryService categoryService,
        ISupplierService supplierService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _supplierService = supplierService;
    }

    // GET: /Products
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return View(products.Select(ProductViewModel.FromDto).ToList());
    }

    // GET: /Products/Create
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ProductViewModel();
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    // POST: /Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _productService.CreateAsync(model.ToDto(), cancellationToken);
                TempData["SuccessMessage"] = $"Producto \"{model.Name.Trim()}\" registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddBusinessError(ex);
            }
        }

        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    // GET: /Products/Edit/5
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        var model = ProductViewModel.FromDto(product);
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    // POST: /Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        if (ModelState.IsValid)
        {
            try
            {
                await _productService.UpdateAsync(model.ToDto(), cancellationToken);
                TempData["SuccessMessage"] = $"Producto \"{model.Name.Trim()}\" actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddBusinessError(ex);
            }
        }

        // Las existencias se muestran como referencia; se vuelven a leer de la base de datos.
        var current = await _productService.GetByIdAsync(id, cancellationToken);
        if (current is null)
            return NotFound();

        model.CurrentStock = current.CurrentStock;
        model.StockStatus = current.StockStatus;
        await LoadOptionsAsync(model, cancellationToken);
        return View(model);
    }

    /// <summary>
    /// Carga las categorías y proveedores activos. Si el producto tiene asignado uno inactivo,
    /// también se incluye para no perder la selección al editar.
    /// </summary>
    private async Task LoadOptionsAsync(ProductViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(cancellationToken);
        model.Categories = categories
            .Where(c => c.IsActive || c.Id == model.CategoryId)
            .Select(c => new SelectListItem(c.IsActive ? c.Name : $"{c.Name} (inactiva)", c.Id.ToString()))
            .ToList();

        var suppliers = await _supplierService.GetAllAsync(cancellationToken);
        model.Suppliers = suppliers
            .Where(s => s.IsActive || s.Id == model.SupplierId)
            .Select(s => new SelectListItem(s.IsActive ? s.Name : $"{s.Name} (inactivo)", s.Id.ToString()))
            .ToList();
    }
}
