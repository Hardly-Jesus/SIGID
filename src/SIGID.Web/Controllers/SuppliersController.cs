using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Gestión de proveedores, HU-07 (crear, consultar, editar y eliminar).
/// </summary>
public class SuppliersController : Controller
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    // GET: /Suppliers
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _supplierService.GetAllAsync(cancellationToken);
        return View(items.Select(SupplierViewModel.FromDto).ToList());
    }

    // GET: /Suppliers/Create
    public IActionResult Create()
    {
        return View(new SupplierViewModel());
    }

    // POST: /Suppliers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _supplierService.CreateAsync(model.ToDto(), cancellationToken);
            TempData["SuccessMessage"] = $"Proveedor \"{model.Name.Trim()}\" registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddBusinessError(ex);
            return View(model);
        }
    }

    // GET: /Suppliers/Edit/5
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return View(SupplierViewModel.FromDto(item));
    }

    // POST: /Suppliers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _supplierService.UpdateAsync(model.ToDto(), cancellationToken);
            TempData["SuccessMessage"] = $"Proveedor \"{model.Name.Trim()}\" actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddBusinessError(ex);
            return View(model);
        }
    }

    // GET: /Suppliers/Delete/5 — pantalla de confirmación
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _supplierService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return View(SupplierViewModel.FromDto(item));
    }

    // POST: /Suppliers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _supplierService.GetByIdAsync(id, cancellationToken);
            await _supplierService.DeleteAsync(id, cancellationToken);
            TempData["SuccessMessage"] = $"Proveedor \"{item?.Name}\" eliminado correctamente.";
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
