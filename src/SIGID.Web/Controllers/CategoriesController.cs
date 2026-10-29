using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGID.Application.Exceptions;
using SIGID.Application.Interfaces.Services;
using SIGID.Web.Extensions;
using SIGID.Web.ViewModels;

namespace SIGID.Web.Controllers;

/// <summary>
/// Gestión de categorías, HU-06 (crear, consultar, editar y eliminar).
/// </summary>
[Authorize]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET: /Categories
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _categoryService.GetAllAsync(cancellationToken);
        return View(items.Select(CategoryViewModel.FromDto).ToList());
    }

    // GET: /Categories/Create
    public IActionResult Create()
    {
        return View(new CategoryViewModel());
    }

    // POST: /Categories/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _categoryService.CreateAsync(model.ToDto(), cancellationToken);
            TempData["SuccessMessage"] = $"Categoría \"{model.Name.Trim()}\" registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddBusinessError(ex);
            return View(model);
        }
    }

    // GET: /Categories/Edit/5
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return View(CategoryViewModel.FromDto(item));
    }

    // POST: /Categories/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _categoryService.UpdateAsync(model.ToDto(), cancellationToken);
            TempData["SuccessMessage"] = $"Categoría \"{model.Name.Trim()}\" actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddBusinessError(ex);
            return View(model);
        }
    }

    // GET: /Categories/Delete/5 — pantalla de confirmación
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        return View(CategoryViewModel.FromDto(item));
    }

    // POST: /Categories/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        try
        {
            var item = await _categoryService.GetByIdAsync(id, cancellationToken);
            await _categoryService.DeleteAsync(id, cancellationToken);
            TempData["SuccessMessage"] = $"Categoría \"{item?.Name}\" eliminada correctamente.";
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
