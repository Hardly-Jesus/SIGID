using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SIGID.Application.DTOs;

namespace SIGID.Web.ViewModels;

/// <summary>
/// Modelo de la vista de productos con las validaciones del formulario (TASK 5.6).
/// </summary>
public class ProductViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "SKU")]
    [Required(ErrorMessage = "El SKU es obligatorio.")]
    [StringLength(50, ErrorMessage = "El SKU no puede superar los {1} caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "El SKU solo admite letras, números, guiones y guiones bajos.")]
    public string SKU { get; set; } = string.Empty;

    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los {1} caracteres.")]
    public string? Description { get; set; }

    [Display(Name = "Categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría.")]
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Display(Name = "Proveedor")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un proveedor.")]
    public int SupplierId { get; set; }

    public string? SupplierName { get; set; }

    [Display(Name = "Unidad de medida")]
    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(20, ErrorMessage = "La unidad de medida no puede superar los {1} caracteres.")]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Display(Name = "Stock mínimo")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
    public int MinimumStock { get; set; }

    [Display(Name = "Stock máximo")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock máximo no puede ser negativo.")]
    public int MaximumStock { get; set; }

    [Display(Name = "Activo")]
    public bool IsActive { get; set; } = true;

    // Listas para los desplegables. Se llenarán cuando existan los servicios
    // de Categorías y Proveedores (sprints posteriores).
    public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> Suppliers { get; set; } = Enumerable.Empty<SelectListItem>();

    public static IEnumerable<SelectListItem> UnitsOfMeasure { get; } = new[]
    {
        new SelectListItem("Unidad (UND)", "UND"),
        new SelectListItem("Kilogramo (KG)", "KG"),
        new SelectListItem("Litro (LT)", "LT"),
        new SelectListItem("Caja (CAJA)", "CAJA"),
        new SelectListItem("Paquete (PAQ)", "PAQ")
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaximumStock < MinimumStock)
        {
            yield return new ValidationResult(
                "El stock máximo debe ser mayor o igual al stock mínimo.",
                new[] { nameof(MaximumStock) });
        }
    }

    public ProductDto ToDto() => new()
    {
        Id = Id,
        SKU = SKU,
        Name = Name,
        Description = Description,
        CategoryId = CategoryId,
        SupplierId = SupplierId,
        UnitOfMeasure = UnitOfMeasure,
        MinimumStock = MinimumStock,
        MaximumStock = MaximumStock,
        IsActive = IsActive
    };

    public static ProductViewModel FromDto(ProductDto dto) => new()
    {
        Id = dto.Id,
        SKU = dto.SKU,
        Name = dto.Name,
        Description = dto.Description,
        CategoryId = dto.CategoryId,
        CategoryName = dto.CategoryName,
        SupplierId = dto.SupplierId,
        SupplierName = dto.SupplierName,
        UnitOfMeasure = dto.UnitOfMeasure,
        MinimumStock = dto.MinimumStock,
        MaximumStock = dto.MaximumStock,
        IsActive = dto.IsActive
    };
}
