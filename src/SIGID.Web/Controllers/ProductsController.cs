using Microsoft.AspNetCore.Mvc;
using SIGID.Application.DTOs;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Gestión de productos (HU-05).
/// Flujo: View → Controller → Service (Application) → Repository (Infrastructure) → SQL Server.
/// El controlador NO accede al DbContext; solo utiliza IProductService.
/// </summary>
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    // GET: /Products
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductDto> products = Array.Empty<ProductDto>();

        try
        {
            products = await _productService.GetAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Mientras la base de datos no esté creada/migrada, la pantalla sigue siendo accesible.
            _logger.LogWarning(ex, "No se pudo obtener el listado de productos.");
            ViewData["DatabaseWarning"] =
                "No se pudo conectar a la base de datos. Verifique la cadena de conexión y ejecute las migraciones.";
        }

        return View(products.Select(ProductViewModel.FromDto).ToList());
    }

    // GET: /Products/Create
    public IActionResult Create()
    {
        return View(new ProductViewModel());
    }

    // POST: /Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _productService.CreateAsync(model.ToDto(), cancellationToken);
            TempData["SuccessMessage"] = "Producto registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // GET: /Products/Edit/5
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        return View(ProductViewModel.FromDto(product));
    }

    // POST: /Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _productService.UpdateAsync(model.ToDto(), cancellationToken);
            TempData["SuccessMessage"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
